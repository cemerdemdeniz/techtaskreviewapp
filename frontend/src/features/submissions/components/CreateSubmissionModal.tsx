import { useState, useCallback, useRef, type DragEvent } from 'react';
import { Modal } from '@/shared/components/ui/Modal';
import { Button } from '@/shared/components/ui/Button';
import { Input } from '@/shared/components/ui/Input';
import { useCreateSubmission, useCreateGitSubmission } from '../api/submissionsApi';

type TabMode = 'upload' | 'git';

interface CreateSubmissionModalProps {
  isOpen: boolean;
  onClose: () => void;
}

// Placeholder candidates - in production this would come from a useCandidates hook
const CANDIDATE_PLACEHOLDER = true;

export function CreateSubmissionModal({ isOpen, onClose }: CreateSubmissionModalProps) {
  const [tab, setTab] = useState<TabMode>('upload');
  const [candidateId, setCandidateId] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const [repositoryUrl, setRepositoryUrl] = useState('');
  const [branch, setBranch] = useState('');
  const [isDragging, setIsDragging] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const createSubmission = useCreateSubmission();
  const createGitSubmission = useCreateGitSubmission();

  const isSubmitting = createSubmission.isPending || createGitSubmission.isPending;

  const resetForm = useCallback(() => {
    setCandidateId('');
    setFile(null);
    setRepositoryUrl('');
    setBranch('');
    setIsDragging(false);
    createSubmission.reset();
    createGitSubmission.reset();
  }, [createSubmission, createGitSubmission]);

  const handleClose = useCallback(() => {
    resetForm();
    onClose();
  }, [resetForm, onClose]);

  const handleDragOver = useCallback((e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const handleDrop = useCallback((e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile && droppedFile.name.endsWith('.zip')) {
      setFile(droppedFile);
    }
  }, []);

  const handleFileChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
    }
  }, []);

  const handleSubmit = useCallback(async () => {
    if (!candidateId) return;

    if (tab === 'upload') {
      if (!file) return;
      await createSubmission.mutateAsync({ candidateId, file });
    } else {
      if (!repositoryUrl) return;
      await createGitSubmission.mutateAsync({
        candidateId,
        repositoryUrl,
        branch: branch || undefined,
      });
    }

    handleClose();
  }, [
    tab,
    candidateId,
    file,
    repositoryUrl,
    branch,
    createSubmission,
    createGitSubmission,
    handleClose,
  ]);

  const canSubmit =
    candidateId &&
    ((tab === 'upload' && file) || (tab === 'git' && repositoryUrl)) &&
    !isSubmitting;

  const mutationError = createSubmission.error || createGitSubmission.error;

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="New Submission" size="lg">
      <div className="space-y-5">
        {/* Candidate Selector */}
        <div>
          <label
            htmlFor="candidate-select"
            className="mb-1.5 block text-sm font-medium text-gray-700"
          >
            Candidate
          </label>
          <select
            id="candidate-select"
            value={candidateId}
            onChange={(e) => setCandidateId(e.target.value)}
            className="input-field w-full"
          >
            <option value="">Select a candidate...</option>
            {/* In production, these would be populated from a candidates API */}
            <option value="placeholder" disabled hidden>
              Loading candidates...
            </option>
          </select>
          <p className="mt-1 text-xs text-gray-500">
            Choose the candidate whose submission you are uploading.
          </p>
        </div>

        {/* Tab Switch */}
        <div className="flex rounded-lg border border-gray-200 p-1 bg-gray-50">
          <button
            type="button"
            onClick={() => setTab('upload')}
            className={`flex-1 rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              tab === 'upload'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <span className="flex items-center justify-center gap-2">
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                />
              </svg>
              Upload ZIP
            </span>
          </button>
          <button
            type="button"
            onClick={() => setTab('git')}
            className={`flex-1 rounded-md px-4 py-2 text-sm font-medium transition-colors ${
              tab === 'git'
                ? 'bg-white text-gray-900 shadow-sm'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <span className="flex items-center justify-center gap-2">
              <svg className="h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 0C5.37 0 0 5.37 0 12c0 5.3 3.438 9.8 8.205 11.387.6.113.82-.258.82-.577 0-.285-.01-1.04-.015-2.04-3.338.724-4.042-1.61-4.042-1.61-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.838 1.236 1.838 1.236 1.07 1.835 2.809 1.305 3.495.998.108-.776.417-1.305.76-1.605-2.665-.3-5.466-1.332-5.466-5.93 0-1.31.465-2.38 1.235-3.22-.135-.303-.54-1.523.105-3.176 0 0 1.005-.322 3.3 1.23.96-.267 1.98-.399 3-.405 1.02.006 2.04.138 3 .405 2.28-1.552 3.285-1.23 3.285-1.23.645 1.653.24 2.873.12 3.176.765.84 1.23 1.91 1.23 3.22 0 4.61-2.805 5.625-5.475 5.92.42.36.81 1.096.81 2.22 0 1.606-.015 2.896-.015 3.286 0 .315.21.69.825.57C20.565 21.795 24 17.295 24 12c0-6.63-5.37-12-12-12z" />
              </svg>
              Git URL
            </span>
          </button>
        </div>

        {/* Upload Tab */}
        {tab === 'upload' && (
          <div>
            <div
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
              onClick={() => fileInputRef.current?.click()}
              className={`cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-colors ${
                isDragging
                  ? 'border-primary-400 bg-primary-50'
                  : file
                    ? 'border-green-300 bg-green-50'
                    : 'border-gray-300 hover:border-gray-400 hover:bg-gray-50'
              }`}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept=".zip"
                onChange={handleFileChange}
                className="hidden"
              />
              {file ? (
                <div className="flex flex-col items-center gap-2">
                  <svg
                    className="h-10 w-10 text-green-500"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <p className="text-sm font-medium text-green-700">{file.name}</p>
                  <p className="text-xs text-green-600">
                    {(file.size / (1024 * 1024)).toFixed(2)} MB
                  </p>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      setFile(null);
                    }}
                    className="text-xs text-red-500 hover:text-red-700 underline"
                  >
                    Remove
                  </button>
                </div>
              ) : (
                <div className="flex flex-col items-center gap-2">
                  <svg
                    className="h-10 w-10 text-gray-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={1.5}
                      d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                    />
                  </svg>
                  <p className="text-sm font-medium text-gray-700">
                    Drop your ZIP file here, or click to browse
                  </p>
                  <p className="text-xs text-gray-500">Only .zip files are accepted</p>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Git Tab */}
        {tab === 'git' && (
          <div className="space-y-4">
            <Input
              label="Repository URL"
              placeholder="https://github.com/user/repo.git"
              value={repositoryUrl}
              onChange={(e) => setRepositoryUrl(e.target.value)}
            />
            <Input
              label="Branch (optional)"
              placeholder="main"
              helperText="Leave blank to use the default branch."
              value={branch}
              onChange={(e) => setBranch(e.target.value)}
            />
          </div>
        )}

        {/* Error */}
        {mutationError && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3">
            <p className="text-sm text-red-700">
              {(mutationError as Error)?.message || 'Failed to create submission.'}
            </p>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-end gap-3 pt-2">
          <Button variant="secondary" onClick={handleClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            loading={isSubmitting}
            disabled={!canSubmit}
          >
            Create Submission
          </Button>
        </div>
      </div>
    </Modal>
  );
}
