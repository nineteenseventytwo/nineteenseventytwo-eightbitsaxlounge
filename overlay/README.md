# Overlay service

The overlay service manages updating the channel broadcast view based on the changing state of the 8-Bit Sax Lounge. It stores views and connects to the broadcasting service in order to update these.

## Implementation
A Node.js service that subscribes to `overlay.*` NATS subjects and forwards events to connected browsers via `socket.io`. The connected browser is OBS which broadcasts the stream. Html stored here defines what broadcast members see with js scripts that respond based on the event.

Usage
- The Node entrypoint (`src/index.js`) sets up an Express webserver and listens on the provided port.
- The generic `OverlayService` handles logic to create the view, currently the `NatsOverlayService` extends this to setup NATS subscription and response on overlay events. Create further extensions and update index.js to handle differently.

- Build & run locally (expects running NATS instance and NATS_USER, NATS_PASS env vars set):
  - `make podman-build`
  - `make podman-run`

Credentials

The overlay service authenticates to NATS with a username/password pair.
In-cluster these are stored centrally in the *state* namespace under the
secret `state-nats-creds`.  The overlay deployment pulls the keys
`overlay_user` and `overlay_pass` from that secret; no separate credential
secret is required.

For local development you can still set the environment variables directly:
```
export NATS_USER=overlay
export NATS_PASS=overlaypw
```
- Deploy to Kubernetes:
  - `make deploy`

- Browser access:
  - kubernetes via the shared platform Gateway (ADR-0012), ClusterIP service port 80->3000:
    - dev: `https://overlay-dev.eightbitsaxlounge.com`
    - prod: `https://overlay.eightbitsaxlounge.com`
  - local:
    - `http://localhost:3000`

Test
- Integration
  - Ensure nats running at `nats://nats:4222`
  - Build image: `make podman-build`
  - Run webserver in local container: `make podman-run`
  - Via browser: `http://localhost:3000` -> should see expected layout
  - Update values: `make test-integration` -> should see values updated

How it works
- Subscribes to `overlay.*` (e.g. `overlay.engine`) and emits socket.io events where event name = `overlay.<tail>` (e.g. `overlay.engine`).
- Image mapping: `engine` images live in `public/images/engine/<name>.svg` (e.g. `engine/lofi.svg`, `engine/room.svg`).
- Shared numeric/level images live in `public/images/values/<n>.svg` — `time`, `delay`, `control1` and `control2` resolve their `value` into this `values` folder (e.g. `public/images/values/3.svg`).

### Client-side assets

The demo pages reference a common stylesheet (`/css/overlay.css`) and script (`/js/overlay.js`).

The JavaScript file contains the logic that listens for socket.io events matching `overlay.*` and updates the DOM accordingly:

* `setPanelText(id, msg)` sets the `textContent` of `#panel-<id>`, pulling `msg.data.value` when available.
* `adjustPanels()` recalculates font sizes for all `.panel-text` elements based on their rendered height, with extra steps for the engine panel to handle long names (≥6, ≥7, ≥8 chars).
* Event handlers are registered for `overlay.engine`, `overlay.time`, `overlay.delay`, `overlay.predelay` (→ delay), `overlay.dial1`, `overlay.control1` (→ dial1), `overlay.dial2`, `overlay.control2` (→ dial2), and `overlay.player`.
* A generic `socket.onAny` logger is wired at the end for debugging.
* Pure functions (`setPanelText`, `adjustPanels`) are exported for Jest unit testing; `init()` only auto-runs in browser context.

## OBS setup 🎛️

Quick steps

1. Add a *Browser Source* in OBS and point the URL to the overlay service:
   - Local dev: `http://localhost:3000/grid.html`
   - In-cluster dev: `https://overlay-dev.eightbitsaxlounge.com/grid.html`
   - In-cluster production: `https://overlay.eightbitsaxlounge.com/grid.html`
   - Requires an OPNsense Unbound host override for `overlay`/`overlay-dev` under
     `eightbitsaxlounge.com` — see nineteenseventytwo-platform's
     `docs/01-network-validation.md`. Without it OBS can't resolve either
     hostname even though the service is up.

2. Recommended Browser Source properties:
   - **Width:** 1920, **Height:** 1080
   - **FPS:** 30
   - **Shutdown source when not visible:** **UNSET** (recommended to keep socket connection)
   - **Refresh browser when scene becomes active:** optional (useful after scene switches)

Troubleshooting

- If the overlay is blank in OBS but works in a normal browser: open `http://localhost:3000/grid.html` in Chrome/Firefox and check devtools for console/network errors.
- Confirm the overlay server is running (logs show `overlay: listening on 3000`) and that NATS is reachable (server logs show `emit event overlay.*`).
- If OBS fails to load remote (HTTPS) overlay, ensure the Gateway's cert is valid and the URL is reachable from the OBS host (DNS override in place, `curl -I <url>` from the OBS machine).
