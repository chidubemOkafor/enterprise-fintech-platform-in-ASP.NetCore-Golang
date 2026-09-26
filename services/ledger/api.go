package main

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"
	"time"
)

type statementRow struct {
	TransactionID string    `json:"transactionId"`
	Amount        string    `json:"amount"`
	EntryType     string    `json:"entryType"`
	OccurredAt    time.Time `json:"occurredAt"`
	RecordedAt    time.Time `json:"recordedAt"`
}

func routes(s *store) *http.ServeMux {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /health", func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusOK)
		w.Write([]byte("Healthy"))
	})

	mux.HandleFunc("GET /api/ledger/accounts/{account}/entries", func(w http.ResponseWriter, r *http.Request) {
		limit := 50
		if v := r.URL.Query().Get("limit"); v != "" {
			parsed, err := strconv.Atoi(v)
			if err != nil || parsed < 1 || parsed > 500 {
				writeJSON(w, http.StatusBadRequest, map[string]string{"message": "limit must be 1-500"})
				return
			}
			limit = parsed
		}

		account := r.PathValue("account")
		rows, err := s.statement(r.Context(), account, limit)
		if err != nil {
			serverError(w, "statement", err)
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{"account": account, "entries": rows})
	})

	mux.HandleFunc("GET /api/ledger/accounts/{account}/balance", func(w http.ResponseWriter, r *http.Request) {
		account := r.PathValue("account")
		balance, count, err := s.balance(r.Context(), account)
		if err != nil {
			serverError(w, "balance", err)
			return
		}
		if count == 0 {
			writeJSON(w, http.StatusNotFound, map[string]string{"message": "no entries for that account"})
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{"account": account, "balance": balance, "entries": count})
	})

	// The whole point of double entry: this has to be zero, always.
	mux.HandleFunc("GET /api/ledger/integrity", func(w http.ResponseWriter, r *http.Request) {
		total, count, err := s.integrity(r.Context())
		if err != nil {
			serverError(w, "integrity", err)
			return
		}
		writeJSON(w, http.StatusOK, map[string]any{
			"total":    total,
			"entries":  count,
			"balanced": total == "0.00" || total == "0",
		})
	})

	return mux
}

func writeJSON(w http.ResponseWriter, status int, body any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if err := json.NewEncoder(w).Encode(body); err != nil {
		log.Printf("write response: %v", err)
	}
}

func serverError(w http.ResponseWriter, what string, err error) {
	log.Printf("%s failed: %v", what, err)
	writeJSON(w, http.StatusInternalServerError, map[string]string{"message": "internal error"})
}
