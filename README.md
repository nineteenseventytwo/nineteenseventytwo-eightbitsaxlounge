# EightBitSaxLounge

A deliberately overengineered Kubernetes-based platform for live music streaming with interactive audience controls. Built for fun, learning, and exploring DevOps/cloud-native patterns.

## What is 8 Bit Sax Lounge?

8 Bit Sax Lounge (8bsl) is a live music stream where viewers can interact with the performance by controlling audio effects in real-time through chat commands. This repository contains the full stack of services powering that experience.

**Is this overengineered?** Absolutely, and intentionally so! This project serves as a hands-on learning platform for Kubernetes, microservices, CI/CD, infrastructure as code, and cloud-native development patterns. What could be a simple script is instead a distributed system running across multiple Raspberry Pis and a PC.

## Architecture Overview

This repository builds container images and runs their tests — it does not own any Kubernetes manifest or deploy anything itself. Every workload's actual cluster definition (Deployments, Services, NetworkPolicies, ExternalSecrets) lives in `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/`, deployed there by Argo CD ([ADR-0012](https://github.com/nineteenseventytwo/nineteenseventytwo-platform/blob/main/docs/decisions/ADR-0012-platform-owns-app-workloads.md)). This split completed in September 2026 — see that repo's `docs/plan/08-eightbitsaxlounge-migration.md` for the migration record.

The system is split into layers:

### **Chat Layer** ([chat/](chat/))
Python-based Twitch chatbot that monitors chat and responds to viewer commands. Supports case-insensitive commands like `!engine`, `!time`, `!delay` to control audio effects. Translates chat commands into MIDI hardware control signals and publishes real-time events to NATS for overlay and monitoring integration.

### **MIDI Layer** ([midi/](midi/))
.NET Minimal API that manages MIDI device communication and abstracts hardware control. Provides RESTful endpoints for controlling audio equipment (currently Ventris Dual Reverb). Handles device state management and MIDI message formatting.

### **Data Layer** ([data/](data/))
Go-based data service providing a RESTful API for MIDI device configurations, presets, and state. Acts as the application's data access layer, abstracting CouchDB operations for other services.

### **DB Layer** ([db/](db/))
CouchDB instance serving as the source of truth for device configurations, presets, and application state. Ensures consistent state across CHAT, MIDI devices, and chat interactions.

For architectural diagrams and visual overviews, see the [diagrams/](diagrams/) folder.

### **State Layer** ([state/](state/))
NATS JetStream event broker for real-time state management across services. Provides persistent message streams and ACL-based routing for event-driven architecture.
- Four JetStream streams: OVERLAY_UPDATES, CHAT_CONTROLS, MIDI_STATE, DATA_API
- Per-service ACL: Chat publishes to overlay.*, MIDI publishes to midi.*, etc.

### **Overlay Layer** ([overlay/](overlay/))
Node.js-based browser overlay service for OBS broadcast integration. Subscribes to overlay state changes from NATS and forwards updates to connected browsers via socket.io for real-time broadcast control.

## Infrastructure

- **Kubernetes Cluster**: `nineteenseventytwo-platform`'s cluster (Raspberry Pi control plane + workers, one PC for MIDI hardware connectivity) — provisioned and owned by that repo, not this one
- **CI/CD**: GitHub Actions — hosted `ubuntu-24.04-arm` runners for build/test/publish; a self-hosted runner with VLAN 20 and Windows-PC reach only for the operations that genuinely need it (`deploy-pc`, the MIDI data-seeding workflows)
- **Deployment**: Argo CD, from `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/`
- **Container Registry**: GitHub Container Registry (ghcr.io)

Each layer has its own build/test/publish pipeline, with releases triggered by version.txt updates.

## Getting Started

Each layer has detailed documentation in its respective README:
- [Chat Layer Documentation](chat/README.md)
- [MIDI Layer Documentation](midi/README.md)
- [Data Layer Documentation](data/README.md)
- [DB Layer Documentation](db/README.md)

## Feature Roadmap
Chat Layer
- ~~ensure dev/prod services not both accessible at once~~ — done, but only
  for chat itself: it's the one layer with genuinely shared state across
  environments (one real Twitch channel, one bot account, one EventSub
  conduit — no such thing as a "dev channel" to isolate against). Every
  other layer runs in both dev and prod simultaneously; only chat toggles,
  via the "Chat Set Active Environment" GitHub Actions workflow. See
  nineteenseventytwo-platform's apps/eightbitsaxlounge/README.md.
- service to update obs resources

Midi Layer
- remove device dependency on PC connection
- make proxy mode more modular

Data Layer
- requests for midi details handled with appropriate response

Db layer
- source of true state -> CHAT and device track

CI/CD
- maintain scripts separately rather than inline
- add flags for skipping tasks e.g. only deploy config, not app etc

Security
- end to end review
- ~~security scanning and monitoring integrated with pipelines~~ — done for
  what this repo owns: `pr.yml`'s `security` job runs Checkov and a Trivy
  filesystem scan on every PR. What used to be `security/`'s own hand-rolled
  Trivy CronJob (PAT-based SARIF upload, one hardcoded image list) is now
  superseded by `nineteenseventytwo-platform`'s `image-vuln-scan.yml` —
  scans every unique image reference actually running cluster-wide,
  weekly, with Slack alerting on findings. That includes every 8bsl image,
  since they're deployed in the same cluster it enumerates.
- secrets managed by ci/cd service vs ansible secrets — done: ExternalSecrets
  Operator against Vault, no ansible-held secrets remain

Cloud replication
- capability to spin-up/down infrastructure outside of midi layer in AWS/Azure/gcp

State Layer?
- unified state to ensure consistent: CHAT and db match midi and chat states
- default state stored and applied as needed
- enforce db as true state of CHAT and midi device
