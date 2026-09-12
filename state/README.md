# Overview

The state layer provides a centralized event message broker for microservice communication using NATS with JetStream stream persistence.

**Architecture:**
- NATS 2.12 with JetStream enabled for message persistence and durability
- Single-node StatefulSet deployment with PersistentVolumeClaim storage

**Event Streams:**
- `OVERLAY_UPDATES` - Overlay state changes (subject: `overlay.>`)
- `CHAT_CONTROLS` - CHAT to system commands (subject: `chat.>`)
- `MIDI_STATE` - MIDI device state observations (subject: `midi.>`)
- `DATA_API` - Data layer events (subject: `data.>`)

**Authentication:**
- Five users with role-based ACL:
  - `system` - Full publish/subscribe (bootstrap operations)
  - `overlay` - Subscribe `overlay.*` (receives all overlay state updates)
  - `chat` - Publish `chat.*` + Publish `overlay.engine`, `overlay.delay`, `overlay.time`, `overlay.dial1`, `overlay.dial2`, `overlay.player`
  - `midi` - Publish `midi.*` + Publish `overlay.engine`, `overlay.predelay`, `overlay.time`, `overlay.control1`, `overlay.control2`
  - `data` - Publish `data.*` only
- Credentials managed via Kubernetes secret `state-nats-creds` with 5 password keys

# Deployment

## Kubernetes Deployment

Containerized NATS server, deployed as a Kubernetes StatefulSet with persistent storage by Argo CD from `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/{dev,prod}/state-statefulset.yaml` (ADR-0012) — this repo builds and tests the image, it does not deploy it. Event streams are automatically initialized at pod startup.

**Configuration:**
- Image: `ghcr.io/nineteenseventytwo/eightbitsaxlounge-state:<version>`, pinned by digest in the platform repo
- StatefulSet: Single replica with 30s readiness probe delay to allow bootstrap completion
- Storage: 1Gi PersistentVolumeClaim (storageClass: longhorn) for JetStream persistence
- Lifecycle: postStart hook runs bootstrap script to create 4 JetStream streams

## Stream Configuration

Each stream configured with:
- Message retention: 1000 messages per stream (configurable)
- Storage backend: File-based (persistent across pod restarts)
- Replicas: 1 (single-node deployment)
- Discard policy: Old messages are discarded when limit reached

**Stream Details:**
```
Stream Name           | Subject Pattern | Max Messages | Purpose
OVERLAY_UPDATES       | overlay.>       | 1000         | Overlay state publishing
CHAT_CONTROLS           | chat.>            | 1000         | CHAT command broadcasting
MIDI_STATE            | midi.>          | 1000         | MIDI device observations
DATA_API              | data.>          | 1000         | Data layer events
```

## Service Configuration

NATS exposed via Kubernetes service `eightbitsaxlounge-state-client` on port 4222 (client connections) and 8222 (monitoring endpoint).

**DNS Names:**
```
In-cluster: eightbitsaxlounge-state-client:4222
In-cluster (specific namespace): eightbitsaxlounge-state-client.<namespace>.svc.cluster.local:4222
```

# CI/CD

**Workflow Trigger:**
- Changes to `state/version.txt` on `main` branch
- Manual workflow dispatch

**Pipeline:**
1. Build Docker image with version tag
2. Test it
3. Push to GitHub Container Registry (GHCR)

Rolling out a new version means bumping the image digest in `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/{dev,prod}/state-statefulset.yaml` and merging — Argo CD applies it from there. `state-nats-creds` is an ExternalSecret against Vault (`kv/tenants/eightbitsaxlounge/state-{dev,prod}`), not a GitHub Actions secret injected by this repo's own pipeline.

**Manual Build and Test:**
```bash
make build              # Validate required files
make build-image        # Build and tag Docker image locally
make test               # Run smoke test with NATS+JetStream
```

# Scope

**Current Implementation:**
- Single-node NATS server with JetStream persistence
- 4 pre-defined event streams with subject-based routing
- Per-service ACL for publish/subscribe restrictions
- Automatic stream initialization at pod startup
- Persistent storage via Kubernetes PersistentVolumeClaim
- HTTP monitoring endpoint on port 8222

**Message Publishing:**
```
Service Pod → NATS Client Connection (nats://host:4222)
           → Authenticate with service user/password
           → Publish to subject (e.g., overlay.state, chat.effect.reverb)
           → Message stored in corresponding JetStream stream
```

**Stream Subscription:**
```
Service Pod → NATS Client Connection
          → Authenticate with service user/password
          → Subscribe to filtered subjects (ACL controls)
          → Receive persisted messages from stream
```

**Testing and Verification:**

Connect to NATS and verify streams exist:
```bash
# From inside pod
kubectl exec -it eightbitsaxlounge-state-0 -- sh

# Inside pod shell, run:
NATS_URL="nats://127.0.0.1:4222"
NATS_USER="system"
NATS_PASS="<system-password-from-secret>"

# List streams
nats --server "$NATS_URL" --user "$NATS_USER" --password "$NATS_PASS" stream ls

# Get stream info
nats --server "$NATS_URL" --user "$NATS_USER" --password "$NATS_PASS" stream info OVERLAY_UPDATES

# Publish test message
nats --server "$NATS_URL" --user "$NATS_USER" --password "$NATS_PASS" \
  pub overlay.test '{"event":"test","timestamp":"2026-01-01T00:00:00Z"}'

# Subscribe to stream messages
nats --server "$NATS_URL" --user "$NATS_USER" --password "$NATS_PASS" \
  sub --all-headers "overlay.>" --from-sequence 1 OVERLAY_UPDATES
```

**Monitoring:**
```bash
# HTTP monitoring endpoint (from pod)
wget -q -O - http://127.0.0.1:8222/varz | head -20

# Event stream monitoring
kubectl logs -f eightbitsaxlounge-state-0
```
