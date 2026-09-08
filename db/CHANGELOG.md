# Changelog
## [2.0.4] - 2026-09-08

### Changed
- Non-root: `USER couchdb` — the base image's own entrypoint drops
  privileges via `setpriv` only when it detects `id -u = 0`; started
  non-root it skips that step and execs directly. Verified end to end:
  single-node setup, `_users`/`_replicator`/`_global_changes` created,
  `_up` answers ok.
- Pinned `couchdb:latest` to `couchdb:3.5` plus a digest, so the image this
  Dockerfile produces is reproducible rather than whatever `latest`
  resolves to on build day.

## [2.0.3] - 2026-02-12

### Changed
- Unified log format: `[timestamp] [Information] [db] message correlationID=<id>`
- Correlation ID propagation from Data layer
- Grafana dashboard improvements for DB logs and health
- Version labels on pods for deployment tracking

## [1.0.1]

### Added
- PersistentVolumeClaim (10GB) for CouchDB data persistence
- Volume mount for `/opt/couchdb/data` in deployment
- PVC deployment step in Ansible playbook

### Changed
- CouchDB data now persists across pod restarts and cluster shutdowns

## [1.0.0]

### Added
- Initial CouchDB deployment for Kubernetes
- Docker image build and GHCR publishing
- Kubernetes manifests (deployment, service, ingress)
- Automated CI/CD via GitHub Actions
- Ansible-based deployment automation
- Environment-specific deployments (dev/prod)
- Host-based ingress routing with sslip.io
