# 交付文件 - ER Model

此文件為交付版 ER Model，來源對齊 `docs/er-model.md`。

```mermaid
erDiagram
    DIRECTORIES ||--o{ DIRECTORIES : contains
    DIRECTORIES ||--o{ FILES : stores
    FILES ||--|| FILE_METADATA : has

    DIRECTORIES {
        uuid id PK
        uuid parent_id FK
        string name
        datetime created_time
        int creation_order
        string physical_path
    }

    FILES {
        uuid id PK
        uuid directory_id FK
        string name
        string extension
        long size_bytes
        datetime created_time
        int file_type
        int creation_order
        string physical_path
    }

    FILE_METADATA {
        uuid file_id PK,FK
        int file_type
        int page_count
        int width
        int height
        string encoding
    }
```

## 指定關係與鍵

- `directories.parent_id -> directories.id`（自關聯）
- `files.directory_id -> directories.id`（一對多）
- `file_metadata.file_id -> files.id`（一對一）
- `file_metadata(file_id, file_type) -> files(id, file_type)`（複合 FK）
