# Changelog

## [4.0.1] - 2026-09-10

### Changed
- Version bump to publish the first container image under the
  `nineteenseventytwo` GHCR org (ADR-0012 platform migration); no image
  exists under the new org for 4.0.0.

## [2.0.0] - 2026-03-13

### Added
- Kubernetes startup probe on `/readyz` (30 × 10s window) to prevent CrashLoopBackOff when the NATS/state layer restarts at the same time as the overlay
- Kubernetes liveness probe on `/healthz` to restart the container if the HTTP server becomes unresponsive
- Kubernetes readiness probe on `/readyz` to remove the pod from service endpoints while NATS is disconnected
- `/healthz` Express endpoint — always 200 while the process is alive
- `/readyz` Express endpoint — 200 when NATS is connected, 503 otherwise
- `NatsOverlayService.isConnected()` method exposing current NATS connection state

### Changed
- `NatsOverlayService` now connects with `waitOnFirstConnect: true` and `maxReconnectAttempts: -1`: retries silently on first connect and reconnects indefinitely, eliminating crashes when NATS is temporarily unavailable
- NATS connection status tracked via the `nc.status()` async iterator; `_connected` flag updated on disconnect, error, and reconnect events
- HTTP server now starts before the NATS connection attempt so health probes are reachable immediately during initialisation
- `overlay.start()` is no longer awaited in the entry point; fatal errors from the service are caught and logged before exiting

## [1.0.4] - 2026-03-11

### Added
- Dynamic font size scaling for engine panel: shorter scale applied for names ≥6, ≥7, ≥8 characters to prevent overflow
- Socket.IO event handlers for MIDI-published overlay subjects: `overlay.predelay` (maps to delay panel), `overlay.control1` (maps to dial1 panel), `overlay.control2` (maps to dial2 panel)
- `overlay.player` socket event handler for player name panel updates
- `chat` panel coordinates added to `TwitchConsole.json` with StreamElements widget alignment data (1920×1080 canvas, position and size derived from background image scale)

### Changed
- Panel rendering switched from image-based (`setImage`/`setText`) to text-based (`setPanelText`) for all panels
- `adjustPanels` now re-runs on every `setPanelText` call and on window resize and DOMContentLoaded for consistent sizing
- Pure functions (`adjustPanels`, `setPanelText`) exported for Jest unit testing; `init()` only auto-calls in browser context

## [0.0.2] - 2026-03-07

### Added
- Node.js-based overlay service for OBS browser source integration
- NATS event subscription to `overlay.*` subjects for real-time broadcast updates
- Socket.IO push events to connected browsers for state synchronization
- Express HTTP server with static file serving from `public/` directory
- Extensible OverlayService architecture with NatsOverlayService implementation
- Docker containerization with multi-stage builds matching Alpine patterns
- Makefile build/test/deploy automation with Docker-based test execution
- GitHub Actions CI/CD workflow using release-template.yaml
- Ansible playbook for Kubernetes deployment with ingress routing
- Ingress-based browser access (dev: overlay-dev.*, prod: overlay.*)
- NATS authentication with overlay user ACL (publish/subscribe to overlay.>)
- In-cluster credential management via state layer secret injection
- Comprehensive testing: Jest unit tests + image asset verification
- Client-side JavaScript for socket.io event handling and DOM updates
- SVG-based image assets for engine types and numeric values
- Integration test support for local development workflows

### Configuration
- Port: 3000 (configurable via PORT environment variable)
- NATS connectivity: Configurable via NATS_URL, NATS_USER, NATS_PASS environment variables
- Service mesh: ClusterIP service on port 80 (maps to 3000)
- Ingress: NGINX controller with automatic host routing based on namespace
