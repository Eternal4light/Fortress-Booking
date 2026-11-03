import { useState } from 'react'
import './App.css'
import { clearSession, getStoredUser, getToken } from './auth'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import PartnersPage from './pages/PartnersPage'
import MyInvitesPage from './pages/MyInvitesPage'
import IssueInvitePage from './pages/IssueInvitePage'
import EmployeesPage from './pages/EmployeesPage'
import ClientsPage from './pages/ClientsPage'

const ROLE_LABELS = {
  Client: 'Клиент',
  Manager: 'Менеджер',
  Admin: 'Администратор',
}

function navClass(isActive) {
  return isActive ? 'nav__item nav__item--active' : 'nav__item'
}

function defaultPageFor(currentUser) {
  if (!currentUser) return 'auth'
  if (currentUser.role === 'Client') return 'myInvites'
  return 'partners'
}

function App() {
  const [user, setUser] = useState(() => (getToken() ? getStoredUser() : null))
  const [authView, setAuthView] = useState('login')
  const [page, setPage] = useState(() => defaultPageFor(getToken() ? getStoredUser() : null))

  const handleLogout = () => {
    clearSession()
    setUser(null)
    setPage('auth')
  }

  const handleAuthenticated = (authenticatedUser) => {
    setUser(authenticatedUser)
    setPage(defaultPageFor(authenticatedUser))
  }

  const isClient = user?.role === 'Client'
  const isAdmin = user?.role === 'Admin'
  // Коворкинги и клиентская база — закрытая информация, видна только сотрудникам сервиса.
  const isStaff = user && (user.role === 'Manager' || user.role === 'Admin')

  return (
    <div className="page">
      <header className="header">
        <div>
          <h1 className="page__title">Fortress Booking</h1>
          <p className="page__subtitle">Коворкинги и приглашения по деятельности</p>
        </div>

        {user ? (
          <div className="header__user">
            <span>
              {user.firstName} {user.lastName} · {ROLE_LABELS[user.role] ?? user.role}
            </span>
            <button type="button" className="link-button" onClick={handleLogout}>
              Выйти
            </button>
          </div>
        ) : (
          <button type="button" className="link-button" onClick={() => setPage('auth')}>
            Войти / Регистрация
          </button>
        )}
      </header>

      <nav className="nav">
        {isStaff && (
          <button className={navClass(page === 'partners')} onClick={() => setPage('partners')}>
            Коворкинги
          </button>
        )}
        {isClient && (
          <button className={navClass(page === 'myInvites')} onClick={() => setPage('myInvites')}>
            Мои приглашения
          </button>
        )}
        {isStaff && (
          <button className={navClass(page === 'issueInvite')} onClick={() => setPage('issueInvite')}>
            Выдать приглашение
          </button>
        )}
        {isStaff && (
          <button className={navClass(page === 'clients')} onClick={() => setPage('clients')}>
            Клиенты
          </button>
        )}
        {isAdmin && (
          <button className={navClass(page === 'employees')} onClick={() => setPage('employees')}>
            Сотрудники
          </button>
        )}
      </nav>

      {page === 'partners' && isStaff && <PartnersPage />}

      {page === 'auth' && !user && authView === 'login' && (
        <LoginPage onAuthenticated={handleAuthenticated} onSwitchToRegister={() => setAuthView('register')} />
      )}
      {page === 'auth' && !user && authView === 'register' && (
        <RegisterPage onAuthenticated={handleAuthenticated} onSwitchToLogin={() => setAuthView('login')} />
      )}

      {page === 'myInvites' && isClient && <MyInvitesPage />}
      {page === 'issueInvite' && isStaff && <IssueInvitePage />}
      {page === 'clients' && isStaff && <ClientsPage />}
      {page === 'employees' && isAdmin && <EmployeesPage />}
    </div>
  )
}

export default App
