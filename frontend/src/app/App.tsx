import { BrowserRouter } from 'react-router-dom';
import { Providers } from '@/app/providers';
import { AppRoutes } from '@/app/routes';
import { ErrorBoundary } from '@/shared/components/ErrorBoundary';

export function App() {
  return (
    <ErrorBoundary>
      <Providers>
        <BrowserRouter>
          <AppRoutes />
        </BrowserRouter>
      </Providers>
    </ErrorBoundary>
  );
}
