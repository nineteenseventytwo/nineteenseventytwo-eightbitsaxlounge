# Changelog

## [2.0.2] - 2026-09-09

### Fixed
- `couchURL()` built the CouchDB connection URL with a raw `fmt.Sprintf`,
  embedding the password into the URL's userinfo component with no
  escaping. Confirmed live: a Vault-generated password containing `/`
  corrupted every request through this function, including the `/health`
  check the readiness/liveness probes depend on — the dev deployment sat
  crash-looping on its startup probe with 503s and no error more specific
  than a generic connection failure. The prod deployment happened to get a
  password with no special characters and never hit it, which is the
  concerning part: this was one password rotation away from breaking
  production too. Rebuilt with `net/url.URL` and `url.UserPassword`, which
  percent-encodes the userinfo component per RFC 3986 — every call site
  still receives a plain string via `.String()`, so nothing downstream
  changes.

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
