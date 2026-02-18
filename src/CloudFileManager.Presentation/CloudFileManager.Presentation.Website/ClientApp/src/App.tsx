import { useEffect, useMemo, useState, type ReactElement } from 'react'
import { TAG_OPTIONS, type DirectoryEntry, type TagOption } from './types/api'
import { useSessionStore } from './store/sessionStore'

interface ExplorerTreeNode {
  name: string
  path: string
  isDirectory: boolean
  children: ExplorerTreeNode[]
  siblingOrder: number
}

function getTagClass(tag: string): string {
  switch (tag) {
    case 'Urgent':
      return 'tag-chip tag-urgent'
    case 'Work':
      return 'tag-chip tag-work'
    case 'Personal':
      return 'tag-chip tag-personal'
    default:
      return 'tag-chip'
  }
}

function getDepth(path: string): number {
  if (path === 'Root') {
    return 0
  }

  return path.split('/').length - 1
}

function getAncestorPaths(path: string): string[] {
  const segments = path.split('/')
  const results: string[] = []

  for (let index = 0; index < segments.length; index += 1) {
    results.push(segments.slice(0, index + 1).join('/'))
  }

  return results
}

function getParentPath(path: string): string {
  if (path === 'Root') {
    return 'Root'
  }

  const segments = path.split('/')
  if (segments.length <= 1) {
    return 'Root'
  }

  return segments.slice(0, segments.length - 1).join('/')
}

function buildExplorerTree(entries: DirectoryEntry[]): ExplorerTreeNode {
  const root: ExplorerTreeNode = {
    name: 'Root',
    path: 'Root',
    isDirectory: true,
    children: [],
    siblingOrder: 0,
  }

  const nodeMap = new Map<string, ExplorerTreeNode>([['Root', root]])

  const sortedEntries = [...entries].sort((left, right) => {
    const leftDepth = getDepth(left.fullPath)
    const rightDepth = getDepth(right.fullPath)
    if (leftDepth !== rightDepth) {
      return leftDepth - rightDepth
    }

    if (left.siblingOrder !== right.siblingOrder) {
      return left.siblingOrder - right.siblingOrder
    }

    return left.name.localeCompare(right.name)
  })

  for (const entry of sortedEntries) {
    const node: ExplorerTreeNode = {
      name: entry.name,
      path: entry.fullPath,
      isDirectory: entry.isDirectory,
      children: [],
      siblingOrder: entry.siblingOrder,
    }

    nodeMap.set(node.path, node)
    const parentPath = getParentPath(node.path)
    const parentNode = nodeMap.get(parentPath)
    if (parentNode) {
      parentNode.children.push(node)
    }
  }

  const sortNodes = (node: ExplorerTreeNode): void => {
    node.children.sort((left, right) => {
      if (left.isDirectory !== right.isDirectory) {
        return left.isDirectory ? -1 : 1
      }

      if (left.siblingOrder !== right.siblingOrder) {
        return left.siblingOrder - right.siblingOrder
      }

      return left.name.localeCompare(right.name)
    })

    for (const child of node.children) {
      sortNodes(child)
    }
  }

  sortNodes(root)
  return root
}

function App() {
  const {
    apiKey,
    state,
    entries,
    treeEntries,
    selectedPath,
    outputLines,
    searchResults,
    tagFindResults,
    xmlContent,
    isBusy,
    toasts,
    initialize,
    setApiKey,
    selectPath,
    refreshCurrentDirectory,
    refreshTreeDirectory,
    changeDirectory,
    setSort,
    copySelected,
    pasteToCurrentDirectory,
    undo,
    redo,
    assignTagForSelected,
    removeTagForSelected,
    createDirectory,
    deleteSelected,
    renameSelected,
    moveSelected,
    uploadFile,
    searchByExtension,
    findByTag,
    exportXml,
    clearXml,
    removeToast,
  } = useSessionStore()

  const [directoryName, setDirectoryName] = useState('')
  const [renameName, setRenameName] = useState('')
  const [moveTargetPath, setMoveTargetPath] = useState('Root')
  const [searchExtension, setSearchExtension] = useState('.txt')
  const [activeTag, setActiveTag] = useState<TagOption>('Work')
  const [selectedUpload, setSelectedUpload] = useState<File | null>(null)
  const [expandedPaths, setExpandedPaths] = useState<Set<string>>(() => new Set(['Root']))
  const [isActionPanelCollapsed, setIsActionPanelCollapsed] = useState(false)

  useEffect(() => {
    void initialize()
  }, [initialize])

  const selectedEntry = useMemo<DirectoryEntry | undefined>(() => {
    return entries.find((entry) => entry.fullPath === selectedPath)
  }, [entries, selectedPath])

  const breadcrumbSegments = useMemo(() => {
    const segments = state.currentDirectoryPath.split('/')
    return segments.map((segment, index) => ({
      label: segment,
      path: segments.slice(0, index + 1).join('/'),
    }))
  }, [state.currentDirectoryPath])

  const explorerTree = useMemo(() => buildExplorerTree(treeEntries), [treeEntries])

  const effectiveExpandedPaths = useMemo(() => {
    const next = new Set(expandedPaths)
    for (const path of getAncestorPaths(state.currentDirectoryPath)) {
      next.add(path)
    }
    return next
  }, [expandedPaths, state.currentDirectoryPath])

  const toggleExpand = (path: string): void => {
    setExpandedPaths((current) => {
      const next = new Set(current)
      if (next.has(path)) {
        next.delete(path)
      } else {
        next.add(path)
      }
      return next
    })
  }

  const renderTreeNode = (node: ExplorerTreeNode): ReactElement => {
    const isExpanded = effectiveExpandedPaths.has(node.path)
    const isSelected = selectedPath === node.path
    const isCurrentDirectory = node.isDirectory && state.currentDirectoryPath === node.path

    return (
      <li key={node.path} className="explorer-node">
        <div className={isSelected || isCurrentDirectory ? 'explorer-row active' : 'explorer-row'}>
          {node.isDirectory ? (
            <button
              className={isExpanded ? 'disclosure expanded' : 'disclosure'}
              onClick={() => toggleExpand(node.path)}
              aria-label={isExpanded ? '收合資料夾' : '展開資料夾'}
            >
              <span className="disclosure-glyph" />
            </button>
          ) : (
            <span className="disclosure-spacer" />
          )}

          <button
            className="explorer-item"
            onClick={() => {
              selectPath(node.path)
              if (node.isDirectory) {
                void changeDirectory(node.path)
              }
            }}
          >
            <span className={node.isDirectory ? 'node-icon folder' : 'node-icon file'} />
            <span>{node.name}</span>
          </button>
        </div>

        {node.isDirectory && isExpanded && node.children.length > 0 && (
          <ul className="explorer-children">{node.children.map((child) => renderTreeNode(child))}</ul>
        )}
      </li>
    )
  }

  const selectedTags = selectedPath ? (state.nodeTags[selectedPath] ?? []) : []
  const undoCount = state.undoStack.length
  const redoCount = state.redoStack.length

  return (
    <div className="app-shell">
      <header className="top-header">
        <div>
          <p className="eyebrow">CloudFileManager · Web Console</p>
          <h1>藍圖式雲端檔案管理</h1>
        </div>
        <div className="api-key-row">
          <label htmlFor="api-key">API Key</label>
          <input
            id="api-key"
            value={apiKey}
            onChange={(event) => setApiKey(event.target.value)}
            placeholder="輸入 X-Api-Key"
          />
        </div>
      </header>

      <section className="command-bar">
        <button onClick={() => void refreshCurrentDirectory()} disabled={isBusy}>
          重新整理
        </button>
        <button onClick={() => void refreshTreeDirectory()} disabled={isBusy}>
          更新樹狀
        </button>
        <button onClick={() => void copySelected()} disabled={!selectedPath || isBusy}>
          複製
        </button>
        <button onClick={() => void pasteToCurrentDirectory()} disabled={isBusy}>
          貼上
        </button>
        <button onClick={() => void undo()} disabled={undoCount === 0 || isBusy}>
          復原 ({undoCount})
        </button>
        <button onClick={() => void redo()} disabled={redoCount === 0 || isBusy}>
          重做 ({redoCount})
        </button>
        <div className="sort-controls">
          <span>排序</span>
          <button onClick={() => void setSort('name', 'asc')} disabled={isBusy}>
            名稱↑
          </button>
          <button onClick={() => void setSort('name', 'desc')} disabled={isBusy}>
            名稱↓
          </button>
          <button onClick={() => void setSort('size', 'desc')} disabled={isBusy}>
            大小↓
          </button>
          <button onClick={() => void setSort('ext', 'asc')} disabled={isBusy}>
            副檔名↑
          </button>
        </div>
      </section>

      <section className="breadcrumb">
        <span>目前位置：</span>
        {breadcrumbSegments.map((segment, index) => (
          <button key={segment.path} onClick={() => void changeDirectory(segment.path)}>
            {segment.label}
            {index < breadcrumbSegments.length - 1 && <span className="separator">/</span>}
          </button>
        ))}
      </section>

      <main className="main-layout">
        <aside className="panel tree-panel">
          <h2>目錄樹</h2>
          <ul className="explorer-tree">{renderTreeNode(explorerTree)}</ul>
        </aside>

        <section className="panel files-panel">
          <h2>目錄內容</h2>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>名稱</th>
                  <th>類型</th>
                  <th>大小</th>
                  <th>副檔名</th>
                  <th>標籤</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((entry) => (
                  <tr
                    key={entry.fullPath}
                    className={selectedPath === entry.fullPath ? 'selected' : ''}
                    onClick={() => {
                      selectPath(entry.fullPath)
                      if (entry.isDirectory) {
                        void changeDirectory(entry.fullPath)
                      }
                    }}
                  >
                    <td>{entry.name}</td>
                    <td>{entry.isDirectory ? '資料夾' : '檔案'}</td>
                    <td>{entry.formattedSize}</td>
                    <td>{entry.extension || '-'}</td>
                    <td>
                      <div className="tag-list">
                        {(state.nodeTags[entry.fullPath] ?? []).map((tag) => (
                          <span key={`${entry.fullPath}-${tag}`} className={getTagClass(tag)}>
                            {tag}
                          </span>
                        ))}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <aside className={isActionPanelCollapsed ? 'panel action-panel collapsed' : 'panel action-panel'}>
          <div className="action-panel-header">
            <h2>操作中心</h2>
            <button
              className="collapse-toggle"
              onClick={() => {
                setIsActionPanelCollapsed((current) => !current)
              }}
            >
              {isActionPanelCollapsed ? '展開' : '收合'}
            </button>
          </div>

          <section className="action-group selection-group">
            <h3>選取資訊</h3>
            <p>{selectedPath ?? '尚未選取項目'}</p>
            <div className="tag-list">
              {selectedTags.map((tag) => (
                <span key={`selected-${tag}`} className={getTagClass(tag)}>
                  {tag}
                </span>
              ))}
            </div>
          </section>

          <section className="action-group create-group compact-group">
            <h3>建立資料夾</h3>
            <div className="inline-row">
              <input value={directoryName} onChange={(event) => setDirectoryName(event.target.value)} placeholder="新資料夾名稱" />
              <button
                onClick={() => {
                  void createDirectory(directoryName)
                  setDirectoryName('')
                }}
                disabled={isBusy}
              >
                建立
              </button>
            </div>
          </section>

          <section className="action-group item-ops-group">
            <h3>選取項目操作</h3>
            <div className="inline-row">
              <input value={renameName} onChange={(event) => setRenameName(event.target.value)} placeholder="重新命名" />
              <button onClick={() => void renameSelected(renameName)} disabled={!selectedPath || isBusy}>
                套用
              </button>
            </div>
            <div className="inline-row">
              <input value={moveTargetPath} onChange={(event) => setMoveTargetPath(event.target.value)} placeholder="目標路徑" />
              <button onClick={() => void moveSelected(moveTargetPath)} disabled={!selectedPath || isBusy}>
                搬移
              </button>
            </div>
            <button className="danger" onClick={() => void deleteSelected()} disabled={!selectedPath || isBusy}>
              刪除選取項目
            </button>
          </section>

          <section className="action-group tags-group compact-group">
            <h3>標籤</h3>
            <div className="inline-row">
              <select value={activeTag} onChange={(event) => setActiveTag(event.target.value as TagOption)}>
                {TAG_OPTIONS.map((tag) => (
                  <option key={tag} value={tag}>
                    {tag}
                  </option>
                ))}
              </select>
              <button onClick={() => void assignTagForSelected(activeTag)} disabled={!selectedPath || isBusy}>
                指派
              </button>
              <button onClick={() => void removeTagForSelected(activeTag)} disabled={!selectedPath || isBusy}>
                移除
              </button>
            </div>
          </section>

          <section className="action-group upload-group compact-group">
            <h3>檔案上傳</h3>
            <div className="inline-row">
              <input
                type="file"
                onChange={(event) => {
                  setSelectedUpload(event.target.files?.[0] ?? null)
                }}
              />
              <button
                onClick={() => {
                  if (selectedUpload) {
                    void uploadFile(selectedUpload)
                    setSelectedUpload(null)
                  }
                }}
                disabled={!selectedUpload || isBusy}
              >
                上傳
              </button>
            </div>
          </section>

          <section className="action-group search-group">
            <h3>搜尋 / 匯出</h3>
            <div className="inline-row">
              <input
                value={searchExtension}
                onChange={(event) => setSearchExtension(event.target.value)}
                placeholder="副檔名，例如 .txt"
              />
              <button onClick={() => void searchByExtension(searchExtension)} disabled={isBusy}>
                搜尋
              </button>
            </div>
            <button onClick={() => void findByTag(activeTag)} disabled={isBusy}>
              以標籤查詢
            </button>
            <button onClick={() => void exportXml()} disabled={isBusy}>
              匯出 XML
            </button>
          </section>
        </aside>
      </main>

      <section className="result-layout">
        <article className="panel">
          <h2>搜尋結果</h2>
          <ul>
            {searchResults.map((item) => (
              <li key={`search-${item}`}>{item}</li>
            ))}
          </ul>
        </article>
        <article className="panel">
          <h2>標籤結果</h2>
          <ul>
            {tagFindResults.map((item) => (
              <li key={`tag-${item}`}>{item}</li>
            ))}
          </ul>
        </article>
        <article className="panel">
          <h2>作業輸出</h2>
          <ul>
            {outputLines.map((line, index) => (
              <li key={`output-${index + 1}`}>{line}</li>
            ))}
          </ul>
        </article>
      </section>

      {xmlContent.length > 0 && (
        <section className="xml-modal">
          <div className="xml-content panel">
            <h2>XML 內容</h2>
            <pre>{xmlContent}</pre>
            <button onClick={clearXml}>關閉</button>
          </div>
        </section>
      )}

      <section className="toast-layer">
        {toasts.map((toast) => (
          <button key={toast.id} className={`toast ${toast.kind}`} onClick={() => removeToast(toast.id)}>
            {toast.message}
          </button>
        ))}
      </section>

      <footer className="status-bar">
        <span>狀態：{isBusy ? '處理中...' : '就緒'}</span>
        <span>目前路徑：{state.currentDirectoryPath}</span>
        <span>剪貼簿：{state.clipboardItem?.path ?? '空'}</span>
        <span>目前排序：{state.currentSortState ? `${state.currentSortState.key}/${state.currentSortState.direction}` : '未設定'}</span>
        <span>
          已選取：{selectedEntry ? `${selectedEntry.name} (${selectedEntry.isDirectory ? '資料夾' : '檔案'})` : '無'}
        </span>
      </footer>
    </div>
  )
}

export default App
