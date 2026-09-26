package main

import (
	"context"
	_ "embed"
	"fmt"

	"github.com/jackc/pgx/v5/pgxpool"
)

//go:embed schema.sql
var schema string

// The issuance account every mint draws from, so the ledger still sums to zero.
const issuanceAccount = "system:issuance"

type store struct {
	pool *pgxpool.Pool
}

func newStore(ctx context.Context, url string) (*store, error) {
	pool, err := pgxpool.New(ctx, url)
	if err != nil {
		return nil, err
	}
	if err := pool.Ping(ctx); err != nil {
		pool.Close()
		return nil, err
	}
	if _, err := pool.Exec(ctx, schema); err != nil {
		pool.Close()
		return nil, fmt.Errorf("apply schema: %w", err)
	}
	return &store{pool: pool}, nil
}

func (s *store) close() {
	s.pool.Close()
}

// post writes both legs of a posting in one transaction. Amounts stay strings all
// the way to Postgres so no float ever touches the money.
func (s *store) post(ctx context.Context, p posting) (int, error) {
	tx, err := s.pool.Begin(ctx)
	if err != nil {
		return 0, err
	}
	defer tx.Rollback(ctx)

	inserted := 0
	for _, leg := range p.legs {
		tag, err := tx.Exec(ctx, `
			INSERT INTO entries (transaction_id, message_id, account, amount, entry_type, occurred_at)
			VALUES ($1, $2, $3, $4::numeric, $5, $6)
			ON CONFLICT (message_id, account) DO NOTHING`,
			p.transactionID, p.messageID, leg.account, leg.amount, p.entryType, p.occurredAt)
		if err != nil {
			return 0, err
		}
		inserted += int(tag.RowsAffected())
	}

	if err := tx.Commit(ctx); err != nil {
		return 0, err
	}
	return inserted, nil
}

func (s *store) statement(ctx context.Context, account string, limit int) ([]statementRow, error) {
	rows, err := s.pool.Query(ctx, `
		SELECT transaction_id, amount::text, entry_type, occurred_at, recorded_at
		FROM entries
		WHERE account = $1
		ORDER BY id DESC
		LIMIT $2`, account, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	out := []statementRow{}
	for rows.Next() {
		var r statementRow
		if err := rows.Scan(&r.TransactionID, &r.Amount, &r.EntryType, &r.OccurredAt, &r.RecordedAt); err != nil {
			return nil, err
		}
		out = append(out, r)
	}
	return out, rows.Err()
}

func (s *store) balance(ctx context.Context, account string) (string, int, error) {
	var balance string
	var count int
	err := s.pool.QueryRow(ctx, `
		SELECT COALESCE(SUM(amount), 0)::text, COUNT(*)
		FROM entries WHERE account = $1`, account).Scan(&balance, &count)
	return balance, count, err
}

// Every posting is balanced, so the sum across all accounts must be exactly zero.
func (s *store) integrity(ctx context.Context) (string, int, error) {
	var total string
	var count int
	err := s.pool.QueryRow(ctx,
		`SELECT COALESCE(SUM(amount), 0)::text, COUNT(*) FROM entries`).Scan(&total, &count)
	return total, count, err
}
