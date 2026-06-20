# InCleanHome Communication Service

> The "great consumer" — receives all integration events and notifies users.

This service contains **2 bounded contexts** in 2 sibling subfolders:
- `Notifications/` — in-app notifications + Firebase Cloud Messaging push.
- `Messaging/` — direct chat via Twilio Conversations + a local message store.

Both BCs share one PostgreSQL database (`communication_db`). Schema:
- `notifications` (Notifications BC)
- `user_devices` (Notifications BC — projection from IAM `UserDeviceTokenUpdatedEvent`)
- `messages` (Messaging BC)

## Endpoints

### Notifications
| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/notifications` | List my notifications |
| GET | `/api/v1/notifications/unread-count` | Unread count |
| PATCH | `/api/v1/notifications/{id}/read` | Mark one as read |
| PATCH | `/api/v1/notifications/read-all` | Mark all as read |
| DELETE | `/api/v1/notifications/{id}` | Delete |
| POST | `/api/v1/notifications/test-send` | Diagnostic — send a test push |

### Messaging
| Method | Path | Purpose |
|---|---|---|
| GET | `/api/v1/messages/token` | Twilio access token |
| POST | `/api/v1/messages/conversation/{userId}` | Get/create Twilio conversation SID |
| GET | `/api/v1/messages/conversations` | List my conversations (legacy local) |
| GET | `/api/v1/messages/{userId}` | Get thread with user (legacy local) |
| POST | `/api/v1/messages/{userId}` | Send a message (legacy local + notification) |
| POST | `/api/v1/messages/{userId}/notify` | Trigger notification after Twilio send |

## Events consumed (this is the main job)

| Event | Source | Action |
|---|---|---|
| `UserDeviceTokenUpdatedEvent` | IAM | Upsert local `user_devices` projection |
| `UserRegisteredEvent` | IAM | Welcome notification |
| `WorkerDocumentsApprovedEvent` | IAM | Notify worker (documents OK) |
| `WorkerDocumentsRejectedEvent` | IAM | Notify worker with reason |
| `UserSuspendedEvent` | IAM | Notify with duration + appeal link |
| `UserSuspensionClearedEvent` | IAM | Notify suspension over |
| `BookingCreatedEvent` | Booking | Notify worker (new request) |
| `BookingConfirmedEvent` | Booking | Notify client (worker accepted) |
| `BookingRejectedEvent` | Booking | Notify client (rejected) |
| `BookingCancelledEvent` | Booking | Notify the other party (+ late note) |
| `BookingCompletedEvent` | Booking | Notify client (pay + review) |
| `PaymentProcessedEvent` | Payment | Notify worker (paid) |
| `PaymentFailedEvent` | Payment | Notify client (try again) |
| `ReviewSubmittedEvent` | Reviews | Notify worker (new review) |
| `ReportSubmittedEvent` | Reviews | Notify reporter (received) |
| `ReportConfirmedEvent` | Reviews | Notify reported user |
| `SuspensionAppealSubmittedEvent` | Reviews | Notify appeal received |

If `RABBITMQ_URL` is the placeholder, consumers don't run (events are dropped).

## External dependencies

| Service | Used for |
|---|---|
| Twilio Conversations | Real-time chat |
| Firebase Cloud Messaging | Push notifications |
| IAM (HTTP) | Resolve recipient role for chat-message notifications |
| Profile (HTTP) | Resolve names/photos in conversation views |

## Environment variables

| Variable | Required | Purpose |
|---|---|---|
| `JWT_SIGNING_KEY` | YES | Same key the gateway/IAM use |
| `COMMUNICATION_DB_CONNECTION` | YES | PostgreSQL connection |
| `RABBITMQ_URL` | recommended | CloudAMQP URL |
| `TWILIO_ACCOUNT_SID` | only if chat | Twilio account |
| `TWILIO_AUTH_TOKEN` | only if chat | Twilio auth token |
| `TWILIO_API_KEY_SID` | only if chat | Twilio API key (for access tokens) |
| `TWILIO_API_KEY_SECRET` | only if chat | Twilio API key secret |
| `TWILIO_CONVERSATIONS_SERVICE_SID` | only if chat | Twilio Conversations Service SID |
| `FIREBASE_CREDENTIALS_JSON` | only if push | Path to firebase-service-account.json |

## Firebase service account

Place the JSON service-account file either:
- in the container at `/app/firebase-service-account.json`, **or**
- set `FIREBASE_CREDENTIALS_JSON` env var to its absolute path.

The file is gitignored — never commit it.

## Run

```bash
cd ../incleanhome-platform
docker compose up --build -d communication-service
```

Direct: http://localhost:5005 · Swagger: http://localhost:5005/swagger
