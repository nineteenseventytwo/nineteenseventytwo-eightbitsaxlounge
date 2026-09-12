# CouchDB Build Resources

This folder builds and tests the CouchDB image this service runs. It does not own the Kubernetes manifests or deploy anything — those live in `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/`, deployed by Argo CD (ADR-0012). See that repo for the actual Deployment, PVC, Service and HTTPRoute definitions.

## Overview

The resources in this folder are designed to:
1. Build a CouchDB Docker image.
2. Test the built image.
3. Publish it to GHCR via a GitHub Actions workflow, triggered by `version.txt`.

## Repository Structure

- **`Dockerfile`**: Defines the steps to build a CouchDB container image.
- **`.github/workflows/db-release.yaml`**: A GitHub Actions workflow to automate the build, test, and publish process.
  - Triggers on updates to `version.txt`
  - Builds and pushes `ghcr.io/<owner>/eightbitsaxlounge-db:<version>` and `:latest`
- **`Makefile`**: build-image/test-image/push targets used by CI
- **`CHANGELOG.md`**: Version history and changes

## Data Persistence

CouchDB data is stored in a PersistentVolumeClaim (10GB) mounted at `/opt/couchdb/data`. This ensures data persists across:
- Pod restarts
- Deployment updates
- Cluster shutdowns

**Note**: When upgrading from a deployment without persistence, existing data will not be automatically migrated. You can either:
1. Re-initialize databases using the MIDI data-init and data-upload workflows
2. Manually backup and restore data before deploying the PVC update

## Prerequisites

Before using these resources, ensure the following:
- Docker is installed and configured on your local machine or runner.
- Kubernetes is installed and configured, with access to the target cluster.
- The following secrets are configured in the GitHub repository settings:
  - `GITHUB_TOKEN`: Automatically provided by GitHub for authenticating with the GitHub Container Registry (GHCR).

## Accessing CouchDB (Fauxton UI)

The Ingress exposes CouchDB via host-based routing using the ingress-nginx external IP (MetalLB) and sslip.io.

- Dev: `http://db-dev.<INGRESS_IP>.sslip.io/_utils/`
- Prod: `http://db.<INGRESS_IP>.sslip.io/_utils/`

Authentication uses the admin credentials configured in the Deployment (password from Secret `secret-db-couchdb`).

To find the IP:
```bash
kubectl -n ingress-nginx get svc ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}{"\n"}'
```

If DNS resolution is unavailable, test with a Host header:
```bash
curl -i -H "Host: db-dev.<IP>.sslip.io" http://<IP>/_utils/
```

## CI/CD and Versioning

- Image version comes from `db/version.txt`.
- CI builds and pushes both `:latest` and `:<version>` to GHCR.
- Bumping the digest in `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/{dev,prod}/db-deployment.yaml` and merging is what actually rolls it out — Argo CD applies it from there.

## Test
```
# Check pod service and endpoint
kubectl -n eightbitsaxlounge-dev get pods -o wide
kubectl -n eightbitsaxlounge-dev get svc db-service
kubectl -n eightbitsaxlounge-dev get endpoints db-service

# Public welcome doc (no auth)
curl -s http://<cluster ip>:5984/

# List DBs (requires admin)
curl -s -u admin:$DB_COUCHDB_PASSWORD http://<cluster ip>:5984/_all_dbs
```

## Monitoring & Logging
- Unified log format: `[timestamp] [Information] [db] message correlationID=<id>`
- Correlation ID is propagated from Data layer for end-to-end tracing in Grafana
- Version labels on pods for deployment tracking