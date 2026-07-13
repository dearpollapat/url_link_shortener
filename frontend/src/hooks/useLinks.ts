import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, type CreateLinkRequest, type LinkStatus } from '../api';

const LINKS_KEY = ['links'] as const;

/** All links + their stats. Owns loading/error/refetch state. */
export function useLinks() {
  return useQuery({
    queryKey: LINKS_KEY,
    queryFn: () => api.list(),
  });
}

export function useCreateLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateLinkRequest) => api.create(body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}

export function useSetStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (vars: { shortCode: string; status: LinkStatus }) =>
      api.setStatus(vars.shortCode, vars.status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}

export function useDeleteLink() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (shortCode: string) => api.remove(shortCode),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: LINKS_KEY }),
  });
}
