import { create } from 'zustand'
import { getErrorMessage, requestJson } from '../api/client'
import {
  EMPTY_STATE,
  type ClientSessionState,
  type DirectoryEntriesResponse,
  type DirectoryEntry,
  type OperationApiResponse,
  type SearchResponse,
  type StatefulApiResponse,
  type TagFindResult,
  type TagOption,
  type ToastKind,
  type ToastMessage,
  type XmlExportResponse,
} from '../types/api'

const DEFAULT_API_KEY = import.meta.env.VITE_API_KEY ?? 'dev-local-api-key'

interface SessionStore {
  apiKey: string
  state: ClientSessionState
  entries: DirectoryEntry[]
  treeEntries: DirectoryEntry[]
  selectedPath: string | null
  outputLines: string[]
  searchResults: string[]
  tagFindResults: string[]
  xmlContent: string
  isBusy: boolean
  toasts: ToastMessage[]
  initialize: () => Promise<void>
  setApiKey: (apiKey: string) => void
  selectPath: (path: string | null) => void
  refreshCurrentDirectory: () => Promise<void>
  refreshTreeDirectory: () => Promise<void>
  changeDirectory: (directoryPath: string) => Promise<void>
  setSort: (key: string, direction: string) => Promise<void>
  copySelected: () => Promise<void>
  pasteToCurrentDirectory: () => Promise<void>
  undo: () => Promise<void>
  redo: () => Promise<void>
  assignTagForSelected: (tag: TagOption) => Promise<void>
  removeTagForSelected: (tag: TagOption) => Promise<void>
  createDirectory: (directoryName: string) => Promise<void>
  deleteSelected: () => Promise<void>
  renameSelected: (newName: string) => Promise<void>
  moveSelected: (targetDirectoryPath: string) => Promise<void>
  uploadFile: (file: File) => Promise<void>
  searchByExtension: (extension: string) => Promise<void>
  findByTag: (tag: TagOption) => Promise<void>
  exportXml: () => Promise<void>
  clearXml: () => void
  removeToast: (id: number) => void
}

function normalizeExtension(input: string): string {
  const value = input.trim()
  if (value.length === 0) {
    return value
  }

  return value.startsWith('.') ? value : `.${value}`
}

export const useSessionStore = create<SessionStore>((set, get) => {
  const addToast = (kind: ToastKind, message: string): void => {
    const toast: ToastMessage = {
      id: Date.now() + Math.floor(Math.random() * 999),
      kind,
      message,
    }

    set((current) => ({
      toasts: [...current.toasts, toast],
    }))

    window.setTimeout(() => {
      get().removeToast(toast.id)
    }, 3200)
  }

  const runStatefulRequest = async <TData>(
    path: string,
    body: Record<string, unknown>,
  ): Promise<StatefulApiResponse<TData> | null> => {
    const apiKey = get().apiKey.trim()
    if (apiKey.length === 0) {
      addToast('error', '請先輸入 API Key。')
      return null
    }

    try {
      const response = await requestJson<StatefulApiResponse<TData>>(path, {
        method: 'POST',
        apiKey,
        body,
      })

      set({
        state: response.state,
        outputLines: response.outputLines,
      })

      if (!response.success) {
        addToast('error', response.message)
        return null
      }

      return response
    } catch (error) {
      addToast('error', getErrorMessage(error))
      return null
    }
  }

  const runOperationRequest = async (
    path: string,
    body: unknown,
    method: 'POST' | 'DELETE' = 'POST',
  ): Promise<boolean> => {
    const apiKey = get().apiKey.trim()
    if (apiKey.length === 0) {
      addToast('error', '請先輸入 API Key。')
      return false
    }

    try {
      const response = await requestJson<OperationApiResponse>(path, {
        method,
        apiKey,
        body,
      })

      if (!response.success) {
        addToast('error', response.message)
        return false
      }

      addToast('success', response.message)
      return true
    } catch (error) {
      addToast('error', getErrorMessage(error))
      return false
    }
  }

  const getSelectedEntry = (selectedPath: string): DirectoryEntry | undefined => {
    return get().entries.find((entry) => entry.fullPath === selectedPath) ?? get().treeEntries.find((entry) => entry.fullPath === selectedPath)
  }

  return {
    apiKey: DEFAULT_API_KEY,
    state: EMPTY_STATE,
    entries: [],
    treeEntries: [],
    selectedPath: null,
    outputLines: [],
    searchResults: [],
    tagFindResults: [],
    xmlContent: '',
    isBusy: false,
    toasts: [],

    initialize: async (): Promise<void> => {
      set({ isBusy: true })
      await get().refreshCurrentDirectory()
      await get().refreshTreeDirectory()
      set({ isBusy: false })
    },

    setApiKey: (apiKey: string): void => {
      set({ apiKey })
    },

    selectPath: (path: string | null): void => {
      set({ selectedPath: path })
    },

    refreshCurrentDirectory: async (): Promise<void> => {
      const state = get().state
      const response = await runStatefulRequest<DirectoryEntriesResponse>('/api/filesystem/directories/entries/query', {
        directoryPath: state.currentDirectoryPath,
        state,
      })
      if (!response) {
        return
      }

      set({
        entries: response.data?.entries ?? [],
        selectedPath: null,
      })
    },

    refreshTreeDirectory: async (): Promise<void> => {
      const state = get().state
      const response = await runStatefulRequest<DirectoryEntriesResponse>('/api/filesystem/directories/entries/query', {
        directoryPath: 'Root',
        state,
      })
      if (!response) {
        return
      }

      set({ treeEntries: response.data?.entries ?? [] })
    },

    changeDirectory: async (directoryPath: string): Promise<void> => {
      set({ isBusy: true })
      const state = get().state
      const response = await runStatefulRequest('/api/filesystem/directories/change-current', {
        directoryPath,
        state,
      })

      if (response) {
        addToast('success', `已切換到 ${directoryPath}`)
        await get().refreshCurrentDirectory()
      }

      set({ isBusy: false })
    },

    setSort: async (key: string, direction: string): Promise<void> => {
      set({ isBusy: true })
      const response = await runStatefulRequest('/api/filesystem/directories/sort', {
        key,
        direction,
        state: get().state,
      })

      if (response) {
        addToast('success', `已套用排序：${key} / ${direction}`)
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }

      set({ isBusy: false })
    },

    copySelected: async (): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取檔案或資料夾。')
        return
      }

      const response = await runStatefulRequest('/api/filesystem/clipboard/copy', {
        sourcePath: selectedPath,
        state: get().state,
      })

      if (response) {
        addToast('success', `已複製：${selectedPath}`)
      }
    },

    pasteToCurrentDirectory: async (): Promise<void> => {
      set({ isBusy: true })
      const response = await runStatefulRequest('/api/filesystem/clipboard/paste', {
        targetDirectoryPath: get().state.currentDirectoryPath,
        state: get().state,
      })

      if (response) {
        addToast('success', response.message)
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }

      set({ isBusy: false })
    },

    undo: async (): Promise<void> => {
      set({ isBusy: true })
      const response = await runStatefulRequest('/api/filesystem/history/undo', { state: get().state })
      if (response) {
        addToast('success', response.message)
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    redo: async (): Promise<void> => {
      set({ isBusy: true })
      const response = await runStatefulRequest('/api/filesystem/history/redo', { state: get().state })
      if (response) {
        addToast('success', response.message)
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    assignTagForSelected: async (tag: TagOption): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取要加標籤的項目。')
        return
      }

      const response = await runStatefulRequest('/api/filesystem/tags/assign', {
        path: selectedPath,
        tag,
        state: get().state,
      })
      if (response) {
        addToast('success', `已標記 ${tag}`)
      }
    },

    removeTagForSelected: async (tag: TagOption): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取要移除標籤的項目。')
        return
      }

      const response = await runStatefulRequest('/api/filesystem/tags/remove', {
        path: selectedPath,
        tag,
        state: get().state,
      })
      if (response) {
        addToast('success', `已移除 ${tag}`)
      }
    },

    createDirectory: async (directoryName: string): Promise<void> => {
      const trimmed = directoryName.trim()
      if (trimmed.length === 0) {
        addToast('info', '請輸入資料夾名稱。')
        return
      }

      set({ isBusy: true })
      const done = await runOperationRequest('/api/filesystem/directories', {
        parentPath: get().state.currentDirectoryPath,
        directoryName: trimmed,
      })

      if (done) {
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    deleteSelected: async (): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取要刪除的項目。')
        return
      }

      const isDirectory = getSelectedEntry(selectedPath)?.isDirectory ?? false
      const endpoint = isDirectory ? '/api/filesystem/directories' : '/api/filesystem/files'
      const payload = isDirectory ? { directoryPath: selectedPath } : { filePath: selectedPath }

      set({ isBusy: true })
      const done = await runOperationRequest(endpoint, payload, 'DELETE')
      if (done) {
        set({ selectedPath: null })
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    renameSelected: async (newName: string): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取要重新命名的項目。')
        return
      }

      const trimmed = newName.trim()
      if (trimmed.length === 0) {
        addToast('info', '請輸入新名稱。')
        return
      }

      const isDirectory = getSelectedEntry(selectedPath)?.isDirectory ?? false
      const endpoint = isDirectory ? '/api/filesystem/directories/rename' : '/api/filesystem/files/rename'
      const payload = isDirectory
        ? { directoryPath: selectedPath, newDirectoryName: trimmed }
        : { filePath: selectedPath, newFileName: trimmed }

      set({ isBusy: true })
      const done = await runOperationRequest(endpoint, payload)
      if (done) {
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    moveSelected: async (targetDirectoryPath: string): Promise<void> => {
      const selectedPath = get().selectedPath
      if (!selectedPath) {
        addToast('info', '請先選取要搬移的項目。')
        return
      }

      const targetPath = targetDirectoryPath.trim()
      if (targetPath.length === 0) {
        addToast('info', '請輸入目標目錄路徑。')
        return
      }

      const isDirectory = getSelectedEntry(selectedPath)?.isDirectory ?? false
      const endpoint = isDirectory ? '/api/filesystem/directories/move' : '/api/filesystem/files/move'
      const payload = isDirectory
        ? { sourceDirectoryPath: selectedPath, targetParentDirectoryPath: targetPath }
        : { sourceFilePath: selectedPath, targetDirectoryPath: targetPath }

      set({ isBusy: true })
      const done = await runOperationRequest(endpoint, payload)
      if (done) {
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      }
      set({ isBusy: false })
    },

    uploadFile: async (file: File): Promise<void> => {
      const apiKey = get().apiKey.trim()
      if (apiKey.length === 0) {
        addToast('error', '請先輸入 API Key。')
        return
      }

      set({ isBusy: true })
      const formData = new FormData()
      formData.append('directoryPath', get().state.currentDirectoryPath)
      formData.append('file', file)

      try {
        const response = await requestJson<OperationApiResponse>('/api/filesystem/files/upload-form', {
          method: 'POST',
          apiKey,
          formData,
        })
        if (!response.success) {
          addToast('error', response.message)
          set({ isBusy: false })
          return
        }

        addToast('success', response.message)
        await get().refreshCurrentDirectory()
        await get().refreshTreeDirectory()
      } catch (error) {
        addToast('error', getErrorMessage(error))
      }

      set({ isBusy: false })
    },

    searchByExtension: async (extension: string): Promise<void> => {
      const normalized = normalizeExtension(extension)
      if (normalized.length === 0) {
        addToast('info', '請輸入副檔名，例如 .txt。')
        return
      }

      const response = await runStatefulRequest<SearchResponse>('/api/filesystem/search/query', {
        extension: normalized,
        directoryPath: get().state.currentDirectoryPath,
        state: get().state,
      })

      if (response) {
        set({ searchResults: response.data?.paths ?? [] })
        addToast('success', `搜尋完成，共 ${response.data?.paths.length ?? 0} 筆。`)
      }
    },

    findByTag: async (tag: TagOption): Promise<void> => {
      const response = await runStatefulRequest<TagFindResult>('/api/filesystem/tags/find', {
        tag,
        directoryPath: get().state.currentDirectoryPath,
        state: get().state,
      })

      if (response) {
        set({ tagFindResults: response.data?.paths ?? [] })
        addToast('success', `標籤查詢完成，共 ${response.data?.paths.length ?? 0} 筆。`)
      }
    },

    exportXml: async (): Promise<void> => {
      const response = await runStatefulRequest<XmlExportResponse>('/api/filesystem/xml/export', {
        directoryPath: get().state.currentDirectoryPath,
        state: get().state,
      })
      if (response) {
        set({ xmlContent: response.data?.xmlContent ?? '' })
        addToast('success', 'XML 匯出完成。')
      }
    },

    clearXml: (): void => {
      set({ xmlContent: '' })
    },

    removeToast: (id: number): void => {
      set((current) => ({
        toasts: current.toasts.filter((item) => item.id !== id),
      }))
    },
  }
})
