# Chat Layer for EightBitSaxLounge

The Chat layer is a Twitch chatbot service that monitors viewer chat and translates commands into real-time audio effect controls. It serves as the primary user interface for audience interaction during live streams.

## Chatbot

A Python-based chatbot that monitors channels and responds to commands, integrating with the MIDI layer to control audio equipment.

Configured for Twitch, but flexible to build bots for other streaming services.

### Development

#### Streaming Service

The app runs an implementation of StreamingBot, currently set to a TwitchIO based Twitch chat bot.

To change to another service create a new impelementation in ./src/bots and update ./src/main.py.

#### Chat Element Response

Chat elements viewers can interact with are mapped to elements defined in ./src/commands. E.g. a viewer in Twitch types in chat !engine room -> the engine command updates the chat service and the 8bsl to the 'room' reverb engine.

Register a new command in ./src/commands/command_registry.py and create a handler in ./src/commands/handlers that implements CommandHandler.

Configured commands (case-insensitive):
- General
    - `!help` - Shows available commands
    - `!player <name>` - Updates player panel on overlay (3-character string, e.g., `!player BOB`)
- Ventris Dual Reverb
    - `!engine <engine name>` - Sets reverb engine A (e.g., room, hall, plate, spring, reverse, modulate, echo)
    - `!time <0-10>` - Set reverb decay time (scales to MIDI 0-127)
    - `!delay <0-10>` - Set reverb pre-delay (scales to MIDI 0-127)
    - `!dial1 <0-10>` - Set custom control 1 (scales to MIDI 0-127)
    - `!dial2 <0-10>` - Set custom control 2 (scales to MIDI 0-127)

#### Event Broadcasting via NATS

The chat layer publishes real-time events to NATS for integration with other services (overlay, monitoring, etc.). When commands execute successfully, the chat bot emits overlay events to JetStream subjects:

- `overlay.engine` - Engine selection updates
- `overlay.time` - Reverb decay time updates
- `overlay.delay` - Reverb pre-delay updates
- `overlay.dial1` - Custom control 1 updates
- `overlay.dial2` - Custom control 2 updates
- `overlay.player` - Player panel updates (via `!player` command)

Events are published asynchronously with lazy connection initialization to the NATS server.

#### App Services

The 8bsl has several services that handle updating music hardware, state data, event publishing, etc. Integration with these services is defined in ./src/services.

Configured services:
- midi_client - this handles requests to update midi data and devices inline with chat element state
- nats_publisher - publishes overlay events to NATS JetStream for real-time state updates
- twitch_client - handles monitoring token validity [deprecated]
- All logs include correlationID for request tracing

#### App config

The streaming bot requires config e.g. channel details, bot account details and these are set in ./src/config

#### Local Development

```bash
# Install dependencies
pip install -r requirements.txt
pip install -r requirements-dev.txt

# Run tests
make test

# Run the bot locally (requires .env file with credentials)
python src/main.py
```

#### Docker (Local)

```bash
# Build and run locally
# Create .env file with required variables first
make docker-build
make docker-run
```

### Deployment

Deployed to Kubernetes with separate dev and prod namespaces. This repo builds,
tests and publishes the image on a `version.txt` change; **Argo CD deploys it**
(platform ADR-0012). The manifests live in
`nineteenseventytwo-platform/apps/eightbitsaxlounge/{dev,prod}/`, and the Argo
application syncs with `selfHeal: true` — so the cluster is whatever Git says,
and changing it any other way is reverted.

**Switch Active Environment:**

Only one environment runs at a time, to prevent duplicate bot messages. Which
one is expressed as the replica count in the manifests, so switching is a
commit in the platform repo:

```yaml
# apps/eightbitsaxlounge/dev/chat-deployment.yaml
replicas: 0        # was 1
# apps/eightbitsaxlounge/prod/chat-deployment.yaml
replicas: 1        # was 0
```

Argo CD picks it up on its next sync; no dispatch and no cluster access needed.

There used to be a "Chat Set Active Environment" workflow that scaled the
Deployments directly. It was removed rather than migrated: `selfHeal: true`
reverts anything it does, and the tenant CI credential deliberately has no
write access to Deployments for exactly that reason. It had also been failing
since the Argo CD migration — it shelled out to `ansible-playbook`, which the
runner image does not carry.

**Manual Deploy:** there is none, by design. The image is published here and
deployed by Argo CD from the platform repo.

### Monitoring & Logging
- Unified log format: `[timestamp] [Information] [chat] message correlationID=<id>`
- Correlation ID is propagated to MIDI and Data layers for end-to-end tracing in Grafana
- Health check endpoints excluded from correlation ID logging
- Version labels on pods for deployment tracking