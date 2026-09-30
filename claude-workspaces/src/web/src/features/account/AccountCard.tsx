import { useQuery } from '@tanstack/react-query'
import { fetchUser, logout } from '../../api/account'
import { Card, ErrorBanner, Spinner } from '../../components/Ui'

export function AccountCard() {
  const { data: user, isPending, error } = useQuery({ queryKey: ['user'], queryFn: fetchUser })

  if (isPending) return <Card title="Account"><Spinner /></Card>
  if (error) return <Card title="Account"><ErrorBanner message={error.message} /></Card>

  const fullName = [user.firstName, user.lastName].filter(Boolean).join(' ')

  return (
    <Card title="Account">
      <dl className="account">
        <dt>Name</dt>
        <dd>{fullName || <span className="muted">Not provided by Google</span>}</dd>
        <dt>Email</dt>
        <dd>{user.email}</dd>
      </dl>
      <button
        onClick={async () => {
          await logout()
          window.location.assign('/')
        }}
      >
        Sign out
      </button>
    </Card>
  )
}
