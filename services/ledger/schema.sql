CREATE TABLE IF NOT EXISTS entries (
    id             bigserial PRIMARY KEY,
    transaction_id uuid NOT NULL,
    message_id     uuid NOT NULL,
    account        text NOT NULL,
    amount         numeric(18,2) NOT NULL CHECK (amount <> 0),
    currency       char(3) NOT NULL DEFAULT 'NGN',
    entry_type     text NOT NULL,
    occurred_at    timestamptz NOT NULL,
    recorded_at    timestamptz NOT NULL DEFAULT now()
);

-- A redelivered message re-inserts the same (message_id, account) pair and is dropped.
CREATE UNIQUE INDEX IF NOT EXISTS entries_message_account_idx ON entries (message_id, account);
CREATE INDEX IF NOT EXISTS entries_account_idx ON entries (account, id);
CREATE INDEX IF NOT EXISTS entries_transaction_idx ON entries (transaction_id);
