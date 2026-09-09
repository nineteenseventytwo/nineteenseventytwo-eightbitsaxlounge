# Changelog

## [2.0.1] - 2026-09-09

### Changed
- No functional change — releases the image built from #65's Dockerfile
  hardening (non-root `app` user, digest-pinned `alpine:3.22`) under the
  new org. `data-release.yaml`'s push trigger only watches
  `data/version.txt`, so #65 never fired it.


## [2.0.0]

### Changed
- Upgraded Go from 1.24 to 1.26.
- Upgraded go-chi to v5.2.5 (latest).

## [1.0.3]

### Changed
- Unified log format across all layers: logs now include correlationID at the end for easier tracing in Grafana.
- INFO-level logging enabled for all API endpoints.
- Health check endpoints now excluded from correlation ID logging.
- Version labels added to all pods for deployment tracking.
- Improved Prometheus/Grafana dashboard queries for pod, node, and deployment health.

### Fixed
- Correlation ID propagation from MIDI to Data layer.
- LogQL queries and dashboard transformations for clearer monitoring tables.

## [Previous]
- See earlier entries for initial API, CouchDB integration, and Kubernetes deployment.
