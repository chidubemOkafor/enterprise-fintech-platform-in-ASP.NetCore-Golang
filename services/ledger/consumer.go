package main

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"strings"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

// MassTransit publishes to a fanout exchange named after the .NET type and wraps the
// payload in an envelope, so these names have to match Contracts/Events exactly.
const (
	fundsMintedExchange       = "Contracts.Events:FundsMinted"
	transferCompletedExchange = "Contracts.Events:TransferCompleted"
	queueName                 = "ledger-entries"
)

type envelope struct {
	MessageID   string          `json:"messageId"`
	MessageType []string        `json:"messageType"`
	Message     json.RawMessage `json:"message"`
}

// json.Number keeps the amount as the literal text that was sent, so 250.50 never
// becomes a float64.
type fundsMinted struct {
	TransactionID  string      `json:"transactionId"`
	AccountNumber  string      `json:"accountNumber"`
	Amount         json.Number `json:"amount"`
	MintedByUserID int         `json:"mintedByUserId"`
	OccurredAt     time.Time   `json:"occurredAt"`
}

type transferCompleted struct {
	TransactionID     string      `json:"transactionId"`
	FromAccountNumber string      `json:"fromAccountNumber"`
	ToAccountNumber   string      `json:"toAccountNumber"`
	Amount            json.Number `json:"amount"`
	OccurredAt        time.Time   `json:"occurredAt"`
}

type leg struct {
	account string
	amount  string
}

type posting struct {
	transactionID string
	messageID     string
	entryType     string
	occurredAt    time.Time
	legs          []leg
}

type consumer struct {
	store *store
	conn  *amqp.Connection
	ch    *amqp.Channel
}

func newConsumer(url string, s *store) (*consumer, error) {
	conn, err := amqp.Dial(url)
	if err != nil {
		return nil, err
	}
	ch, err := conn.Channel()
	if err != nil {
		conn.Close()
		return nil, err
	}
	if _, err := ch.QueueDeclare(queueName, true, false, false, false, nil); err != nil {
		conn.Close()
		return nil, err
	}
	for _, exchange := range []string{fundsMintedExchange, transferCompletedExchange} {
		if err := ch.ExchangeDeclare(exchange, "fanout", true, false, false, false, nil); err != nil {
			conn.Close()
			return nil, err
		}
		if err := ch.QueueBind(queueName, "", exchange, false, nil); err != nil {
			conn.Close()
			return nil, err
		}
	}
	if err := ch.Qos(10, 0, false); err != nil {
		conn.Close()
		return nil, err
	}
	return &consumer{store: s, conn: conn, ch: ch}, nil
}

func (c *consumer) close() {
	c.conn.Close()
}

func (c *consumer) run(ctx context.Context) error {
	deliveries, err := c.ch.Consume(queueName, "ledger", false, false, false, false, nil)
	if err != nil {
		return err
	}
	log.Printf("consuming %s", queueName)

	for {
		select {
		case <-ctx.Done():
			return nil
		case d, ok := <-deliveries:
			if !ok {
				return fmt.Errorf("delivery channel closed")
			}
			c.handle(ctx, d)
		}
	}
}

func (c *consumer) handle(ctx context.Context, d amqp.Delivery) {
	p, err := parse(d.Body)
	if err != nil {
		// Nothing about a redelivery would fix a message we cannot read.
		log.Printf("dropping unreadable message: %v", err)
		d.Nack(false, false)
		return
	}

	inserted, err := c.store.post(ctx, p)
	if err != nil {
		log.Printf("posting %s failed, requeueing: %v", p.transactionID, err)
		d.Nack(false, true)
		return
	}

	if inserted == 0 {
		log.Printf("%s already posted, skipping", p.messageID)
	} else {
		log.Printf("posted %s %s (%d legs)", p.entryType, p.transactionID, inserted)
	}
	d.Ack(false)
}

func parse(body []byte) (posting, error) {
	var env envelope
	if err := json.Unmarshal(body, &env); err != nil {
		return posting{}, fmt.Errorf("envelope: %w", err)
	}
	if env.MessageID == "" {
		return posting{}, fmt.Errorf("envelope has no messageId")
	}

	switch {
	case matches(env.MessageType, "FundsMinted"):
		var m fundsMinted
		if err := json.Unmarshal(env.Message, &m); err != nil {
			return posting{}, fmt.Errorf("FundsMinted: %w", err)
		}
		if err := validAmount(m.Amount); err != nil {
			return posting{}, err
		}
		return posting{
			transactionID: m.TransactionID,
			messageID:     env.MessageID,
			entryType:     "mint",
			occurredAt:    m.OccurredAt,
			legs: []leg{
				{account: issuanceAccount, amount: "-" + m.Amount.String()},
				{account: m.AccountNumber, amount: m.Amount.String()},
			},
		}, nil

	case matches(env.MessageType, "TransferCompleted"):
		var m transferCompleted
		if err := json.Unmarshal(env.Message, &m); err != nil {
			return posting{}, fmt.Errorf("TransferCompleted: %w", err)
		}
		if err := validAmount(m.Amount); err != nil {
			return posting{}, err
		}
		return posting{
			transactionID: m.TransactionID,
			messageID:     env.MessageID,
			entryType:     "transfer",
			occurredAt:    m.OccurredAt,
			legs: []leg{
				{account: m.FromAccountNumber, amount: "-" + m.Amount.String()},
				{account: m.ToAccountNumber, amount: m.Amount.String()},
			},
		}, nil
	}

	return posting{}, fmt.Errorf("unhandled messageType %v", env.MessageType)
}

func matches(messageTypes []string, name string) bool {
	for _, t := range messageTypes {
		if strings.HasSuffix(t, ":"+name) {
			return true
		}
	}
	return false
}

func validAmount(n json.Number) error {
	f, err := n.Float64()
	if err != nil {
		return fmt.Errorf("amount %q is not a number", n.String())
	}
	if f <= 0 {
		return fmt.Errorf("amount %q is not positive", n.String())
	}
	return nil
}
