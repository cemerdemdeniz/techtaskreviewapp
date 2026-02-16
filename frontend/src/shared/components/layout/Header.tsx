import { useAuthStore } from '@/features/auth/store/authStore';
import { Button } from '@/shared/components/ui/Button';

export function Header() {
  const { user, logout } = useAuthStore();

  return (
    <header className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6">
      <div />
      <div className="flex items-center gap-4">
        {user && (
          <span className="text-sm text-gray-600">
            Signed in as <span className="font-medium text-gray-900">{user.email}</span>
          </span>
        )}
        <Button variant="ghost" size="sm" onClick={logout}>
          Sign out
        </Button>
      </div>
    </header>
  );
}
