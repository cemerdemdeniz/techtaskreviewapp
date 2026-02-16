import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Button } from '@/shared/components/ui/Button';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('ErrorBoundary caught:', error, errorInfo);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div className="flex min-h-screen items-center justify-center bg-gray-50 p-6">
          <div className="max-w-md text-center">
            <h1 className="mb-2 text-2xl font-bold text-gray-900">
              Something went wrong
            </h1>
            <p className="mb-6 text-gray-600">
              {this.state.error?.message || 'An unexpected error occurred.'}
            </p>
            <Button onClick={this.handleReset}>Try again</Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
