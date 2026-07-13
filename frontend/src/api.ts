// API client for the URL Shortener backend. The base URL is configurable via
// VITE_API_BASE so the frontend can point at a different host in other setups.
const API_BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

export type LinkStatus = 'Active' | 'Disabled';

export interface LinkResponse {
  shortCode: string;
  shortUrl: string;
  destinations: Record<string, string>;
  status: LinkStatus;
  clickCount: number;
  createdAt: string;
  lastAccessedAt: string | null;
}

export interface CreateLinkRequest {
  url: string;
  customAlias?: string;
  destinations?: Record<string, string>;
}

/** Error carrying the human-readable detail from an RFC 7807 ProblemDetails body. */
export class ApiError extends Error {
  constructor(message: string, readonly status: number) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

  if (!response.ok) {
    let detail = `Request failed (${response.status})`;
    try {
      const problem = await response.json();
      detail = problem.detail ?? problem.title ?? detail;
    } catch {
      /* non-JSON error body — keep the default message */
    }
    throw new ApiError(detail, response.status);
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  list: () => request<LinkResponse[]>('/api/links'),

  create: (body: CreateLinkRequest) =>
    request<LinkResponse>('/api/links', {
      method: 'POST',
      body: JSON.stringify(body),
    }),

  setStatus: (shortCode: string, status: LinkStatus) =>
    request<LinkResponse>(`/api/links/${encodeURIComponent(shortCode)}`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),

  remove: (shortCode: string) =>
    request<void>(`/api/links/${encodeURIComponent(shortCode)}`, {
      method: 'DELETE',
    }),
};
