-- Ollama (nomic-embed-text, 768 dims) के लिए नया table.
-- पुराना document_chunks (1536 dims, OpenAI) वैसे ही रहता है — Ai:Provider से switch होता है.
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS document_chunks_768 (
    id         bigserial PRIMARY KEY,
    source     text NOT NULL,
    content    text NOT NULL,
    embedding  vector(768),
    created_at timestamptz DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_document_chunks_768_embedding_hnsw
    ON document_chunks_768 USING hnsw (embedding vector_cosine_ops);
