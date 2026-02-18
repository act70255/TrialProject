export type ToastKind = 'success' | 'error' | 'info'

export interface SessionClipboardItem {
  isDirectory: boolean
  path: string
}

export interface SessionSortState {
  key: string
  direction: string
}

export interface SessionUndoAction {
  kind: string
  previousSortState?: SessionSortState
  currentSortState?: SessionSortState
  nodePath?: string
  tagName?: string
  tagColor?: string
}

export interface ClientSessionState {
  currentDirectoryPath: string
  clipboardItem?: SessionClipboardItem
  currentSortState?: SessionSortState
  nodeTags: Record<string, string[]>
  undoStack: SessionUndoAction[]
  redoStack: SessionUndoAction[]
}

export interface DirectoryEntry {
  name: string
  isDirectory: boolean
  fullPath: string
  sizeBytes: number
  formattedSize: string
  extension: string
  siblingOrder: number
}

export interface StatefulApiResponse<TData> {
  success: boolean
  message: string
  errorCode?: string
  data?: TData
  state: ClientSessionState
  outputLines: string[]
}

export interface OperationApiResponse {
  success: boolean
  message: string
  errorCode?: string
}

export interface DirectoryEntriesResponse {
  isFound: boolean
  entries: DirectoryEntry[]
}

export interface SearchResponse {
  paths: string[]
  traverseLog: string[]
}

export interface XmlExportResponse {
  xmlContent: string
  outputPath?: string
}

export interface TagFindResult {
  tag: string
  color: string
  scopePath: string
  paths: string[]
}

export interface ToastMessage {
  id: number
  kind: ToastKind
  message: string
}

export const EMPTY_STATE: ClientSessionState = {
  currentDirectoryPath: 'Root',
  clipboardItem: undefined,
  currentSortState: undefined,
  nodeTags: {},
  undoStack: [],
  redoStack: [],
}

export const TAG_OPTIONS = ['Urgent', 'Work', 'Personal'] as const
export type TagOption = (typeof TAG_OPTIONS)[number]
