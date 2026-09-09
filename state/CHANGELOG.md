# Changelog

## [0.0.21] - 2026-09-09

### Changed
- Non-root: new `nats-run` user (uid/gid 1000) — this base ships no
  dedicated user at all, confirmed live (default id is uid=0(root), no
  `nats` entry in /etc/passwd or /etc/group).
- Pinned `natsio/nats-box:latest` and `nats:2-alpine` to specific versions
  plus digests (0.14.5, 2.14.6-alpine — confirmed via `nats-server -v`
  before pinning, not guessed).


## [0.0.19] - 2026-03-09

### Changed
- chat user ACL: Added `overlay.player` to publish permissions
- midi user ACL: Added overlay subject publish permissions — `overlay.engine`, `overlay.predelay`, `overlay.time`, `overlay.control1`, `overlay.control2` — to support MIDI layer publishing overlay state directly

## [0.0.17] - 2026-03-07

### Changed
- chat user ACL: Added explicit publish permissions to overlay state subjects (overlay.engine, overlay.delay, overlay.time, overlay.dial1, overlay.dial2)
- Clarified subject namespace separation: Chat publishes to overlay.* subjects for broadcast control

### Fixed
- Overlay user ACL: Changed subscribe permission from `chat.effect>` to `overlay.>` to allow overlay service to receive broadcast state updates
- Direct subject publishing: Overlay service now subscribes to `overlay.*` subjects published by Chat layer

## [0.0.15] - 2026-03-07

### Added
- NATS 2.12 JetStream state layer with centralized event message broker
- Four JetStream event streams: OVERLAY_UPDATES, CHAT_CONTROLS, MIDI_STATE, DATA_API
- Per-service ACL configuration with role-based publish/subscribe restrictions
  - System user: Full publish/subscribe (bootstrap operations)
  - Service users (overlay, chat, midi, data): Restricted publish to service subjects
- Custom Alpine Docker image with baked-in nats-cli and bootstrap tooling
- Entrypoint script for runtime password substitution via sed into nats.conf template
- PostStart lifecycle hook for automatic JetStream stream creation at pod startup
- Kubernetes StatefulSet deployment with PersistentVolumeClaim storage (1Gi, longhorn)
- HTTP monitoring endpoint on port 8222 for cluster health checks
- Makefile build/test/deploy automation matching other layers
- GitHub Actions CI/CD workflow with secret injection for 5 service passwords
- Ansible playbook for credential injection and versioned image deployment
- Bootstrap script with HTTP readiness monitoring (wget) and nats CLI stream creation
- Comprehensive documentation and verification procedures

### Changed
- Image versioning pattern: Static manifest with `imagePullPolicy: Always`, versioned tag updated via `kubectl set image` after apply (follows midi-api-deploy pattern)
- Readiness probe: 35s initialDelaySeconds to allow bootstrap completion
- Bootstrap readiness check: HTTP-based monitoring instead of NATS-specific commands

### Fixed
- NATS CLI authentication: Using long-form `--user` and `--password` flags with proper shell variable substitution for special characters
- Stream creation: Added `--defaults` flag to skip interactive prompts in postStart hook
- CLI compatibility: Removed unsupported `-n` flag from nats stream add commands
- Password substitution: Fixed sed replacement patterns to handle special regex characters
