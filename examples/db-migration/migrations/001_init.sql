-- Migration: 001_init
-- Create initial schema

-- UP
CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP
);

INSERT OR IGNORE INTO settings (key, value) VALUES ('version', '1.0.0');
INSERT OR IGNORE INTO settings (key, value) VALUES ('initialized', datetime('now'));

-- DOWN
-- DROP TABLE settings;
