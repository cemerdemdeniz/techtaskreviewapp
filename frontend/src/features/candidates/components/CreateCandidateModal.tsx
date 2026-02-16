import { useState } from 'react';
import { Modal } from '@/shared/components/ui/Modal';
import { Input } from '@/shared/components/ui/Input';
import { Button } from '@/shared/components/ui/Button';
import { useCreateCandidate } from '../api/candidatesApi';
import type { CreateCandidateRequest } from '../types';

interface CreateCandidateModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export function CreateCandidateModal({
  isOpen,
  onClose,
}: CreateCandidateModalProps) {
  const [form, setForm] = useState<CreateCandidateRequest>({
    firstName: '',
    lastName: '',
    email: '',
    role: 'Frontend',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createMutation = useCreateCandidate();

  const validate = (): boolean => {
    const newErrors: Record<string, string> = {};
    if (!form.firstName.trim()) newErrors.firstName = 'First name is required';
    if (!form.lastName.trim()) newErrors.lastName = 'Last name is required';
    if (!form.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
      newErrors.email = 'Invalid email format';
    }
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    try {
      await createMutation.mutateAsync(form);
      setForm({ firstName: '', lastName: '', email: '', role: 'Frontend' });
      setErrors({});
      onClose();
    } catch {
      // Error is handled by mutation state
    }
  };

  const handleClose = () => {
    setForm({ firstName: '', lastName: '', email: '', role: 'Frontend' });
    setErrors({});
    onClose();
  };

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Add New Candidate" size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Input
            label="First Name"
            value={form.firstName}
            onChange={(e) =>
              setForm((prev) => ({ ...prev, firstName: e.target.value }))
            }
            error={errors.firstName}
            placeholder="John"
          />
          <Input
            label="Last Name"
            value={form.lastName}
            onChange={(e) =>
              setForm((prev) => ({ ...prev, lastName: e.target.value }))
            }
            error={errors.lastName}
            placeholder="Doe"
          />
        </div>

        <Input
          label="Email"
          type="email"
          value={form.email}
          onChange={(e) =>
            setForm((prev) => ({ ...prev, email: e.target.value }))
          }
          error={errors.email}
          placeholder="john.doe@example.com"
        />

        <div className="w-full">
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            Role
          </label>
          <select
            value={form.role}
            onChange={(e) =>
              setForm((prev) => ({
                ...prev,
                role: e.target.value as 'Frontend' | 'Backend',
              }))
            }
            className="input-field w-full"
          >
            <option value="Frontend">Frontend</option>
            <option value="Backend">Backend</option>
          </select>
        </div>

        {createMutation.isError && (
          <p className="text-sm text-red-600">
            Failed to create candidate. Please try again.
          </p>
        )}

        <div className="flex justify-end gap-3 pt-2">
          <Button type="button" variant="secondary" onClick={handleClose}>
            Cancel
          </Button>
          <Button type="submit" loading={createMutation.isPending}>
            Create Candidate
          </Button>
        </div>
      </form>
    </Modal>
  );
}
