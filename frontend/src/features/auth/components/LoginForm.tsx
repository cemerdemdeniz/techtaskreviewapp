import { useState, type FormEvent } from 'react';
import { Navigate } from 'react-router-dom';
import { useLogin } from '@/features/auth/hooks/useAuth';
import { useAuthStore } from '@/features/auth/store/authStore';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import { Card } from '@/shared/components/ui/Card';

export function LoginForm() {
  const { isAuthenticated } = useAuthStore();
  const login = useLogin();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    login.mutate({ email, password });
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-primary-600">
            <span className="text-lg font-bold text-white">TR</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">TechTask Review</h1>
          <p className="mt-2 text-gray-600">Sign in to your account</p>
        </div>

        <Card>
          <form onSubmit={handleSubmit} className="space-y-4">
            <Input
              label="Email address"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              autoComplete="email"
            />

            <Input
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
              autoComplete="current-password"
            />

            {login.isError && (
              <div className="rounded-lg bg-red-50 p-3 text-sm text-red-700">
                Invalid email or password. Please try again.
              </div>
            )}

            <Button
              type="submit"
              loading={login.isPending}
              className="w-full"
            >
              Sign in
            </Button>
          </form>
        </Card>
      </div>
    </div>
  );
}
