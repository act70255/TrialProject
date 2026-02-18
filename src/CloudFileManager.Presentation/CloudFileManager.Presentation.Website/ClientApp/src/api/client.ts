import type { OperationApiResponse } from '../types/api'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5181'

export class ApiRequestError extends Error {
  public readonly status: number

  public constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE'
  apiKey: string
  body?: unknown
  formData?: FormData
}

export async function requestJson<TResponse>(path: string, options: RequestOptions): Promise<TResponse> {
  const headers = new Headers()
  headers.set('X-Api-Key', options.apiKey)

  if (!options.formData) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? 'GET',
    headers,
    body: options.formData ?? (options.body !== undefined ? JSON.stringify(options.body) : undefined),
  })

  const payload = await response.json().catch(() => ({} as Record<string, unknown>))

  if (!response.ok) {
    const message =
      (payload as Partial<OperationApiResponse>).message ??
      (payload as { title?: string }).title ??
      `請求失敗（HTTP ${response.status}）`
    throw new ApiRequestError(message, response.status)
  }

  return payload as TResponse
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiRequestError) {
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return '發生未知錯誤。'
}
