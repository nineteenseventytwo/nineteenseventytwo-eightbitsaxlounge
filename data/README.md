# Eightbitsaxlounge Data API

A Go-based REST API data layer that provides CouchDB integration through Kubernetes services with NGINX ingress routing.

## Architecture Overview

```
External Client → NGINX Ingress (host-based) → K8s Service → Go API Pods → CouchDB
```

The data layer consists of:
- **Go REST API**: Handles HTTP requests and CouchDB operations
- **Kubernetes Service**: Load balances across API replicas (ClusterIP)
- **NGINX Ingress (host-based)**: Routes external traffic using dedicated hostnames (e.g. `data-dev.<IP>.sslip.io` / `data.<IP>.sslip.io`)
- **CouchDB Integration**: Connects to existing `db-service:5984`

## API Development
The API now uses host-based routing and exposes database & document endpoints at the root path (no `/data` prefix). The service is instantiated in `main.go` and injected in `routes.go`. A handler for each route decodes the URL params and calls the relevant CRUD service operation.

1. Define routes in routes.go e.g. GET/POST and link to handler
2. Create handler in handlers.go to implement route capturing url params
3. Create method in service interface couchservice.go
4. Implement method in service code couchdb.go
5. Add handler and service tests for new method

## API Routes (host-based, root path)

### Database Operations
| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/{dbname}` | Create a new database |
| `GET` | `/{dbname}` | Get database info (doc count, etc.) |
| `DELETE` | `/{dbname}` | Delete the database |

### Document Operations
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/{dbname}/docs` | List all documents in the database |
| `GET` | `/{dbname}/{id}` | Get document by ID |
| `POST` | `/{dbname}` | Create new document |
| `PUT` | `/{dbname}/{id}` | Update existing document |
| `DELETE` | `/{dbname}/{id}` | Delete document by ID |

### Example Usage
```bash
# Create database
curl -X PUT http://data-dev.<IP>.sslip.io/mydb

# Get database info
curl -X GET http://data-dev.<IP>.sslip.io/mydb

# Create document
curl -X POST http://data-dev.<IP>.sslip.io/mydb \
  -H "Content-Type: application/json" \
  -d '{"name": "example", "value": 123}'

# Get document
curl -X GET http://data-dev.<IP>.sslip.io/mydb/doc123

# List all documents in the database
curl -X GET http://data-dev.<IP>.sslip.io/mydb/docs

# Update document
curl -X PUT http://data-dev.<IP>.sslip.io/mydb/doc123 \
  -H "Content-Type: application/json" \
  -d '{"_id": "doc123", "_rev": "1-abc", "name": "updated"}'

# Delete document
curl -X DELETE http://data-dev.<IP>.sslip.io/mydb/doc123
```

## Requirements
- CouchDB layer deployed with ClusterIP service `db-service:5984`
- Kubernetes secret `secret-db-couchdb` containing CouchDB password

## Deployment
This repo builds and tests the image; it does not deploy it. Deployment is Argo CD, from `nineteenseventytwo-platform`'s `apps/eightbitsaxlounge/{dev,prod}/data-deployment.yaml` (ADR-0012).

Notes on image versioning:
- Images are tagged from `data/version.txt` during CI and pushed as both `:latest` and `:$VERSION`.
- Rolling out a version means bumping the digest in the platform repo's manifest and merging — Argo CD applies it from there.

## Testing
```bash
cd data/go
go test -v
```

## Configuration
- Environment variables for CouchDB are set in the Deployment:
  - `COUCHDB_ENDPOINT` (e.g., `db-service:5984`)
  - `COUCHDB_USER`
  - `COUCHDB_PASSWORD` (from secret `secret-db-couchdb`)
- Ingress uses host-based routing; all requests at path root `/` for the host are routed to the API Service.

## Monitoring & Logging
- Unified log format: `[timestamp] [Information] [data] message correlationID=<id>`
- Correlation ID is propagated from MIDI layer for end-to-end tracing in Grafana
- Health check endpoints excluded from correlation ID logging
- Version labels on pods for deployment tracking