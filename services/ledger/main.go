package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"
)

func main() {
	dbURL := env("LEDGER_DATABASE_URL", "postgres://ledger_user:securepassword@localhost:5455/ledger_db")
	rabbitURL := env("LEDGER_RABBITMQ_URL", "amqp://admin:password123@localhost:5672/")
	addr := env("LEDGER_ADDR", ":8084")

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	store, err := newStore(ctx, dbURL)
	if err != nil {
		log.Fatalf("database: %v", err)
	}
	defer store.close()

	consumer, err := newConsumer(rabbitURL, store)
	if err != nil {
		log.Fatalf("rabbitmq: %v", err)
	}
	defer consumer.close()

	go func() {
		if err := consumer.run(ctx); err != nil {
			log.Printf("consumer stopped: %v", err)
			stop()
		}
	}()

	server := &http.Server{
		Addr:              addr,
		Handler:           routes(store),
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		log.Printf("ledger listening on %s", addr)
		if err := server.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
			log.Printf("http server: %v", err)
			stop()
		}
	}()

	<-ctx.Done()
	log.Println("shutting down")

	shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()
	server.Shutdown(shutdownCtx)
}

func env(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
