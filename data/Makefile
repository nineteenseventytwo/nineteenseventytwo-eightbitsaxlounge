# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-data
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Lint Go code
lint:
	@echo "Running Go linter..."
	docker run --rm -v "$(shell pwd):/data" -w /data/go golangci/golangci-lint:latest golangci-lint run ./...

# Run Go tests in container
test:
	@echo "Running Go tests..."
	docker run --rm -v "$(shell pwd):/data" -w /data/go golang:1.26-alpine go test ./...
	@echo "All tests passed"

test-podman:
	@echo "Running Go tests..."
	podman run --rm -v "$(shell pwd):/data" -w /data/go golang:1.26-alpine go test ./...
	@echo "All tests passed"

# Build Docker image locally
build-image:
	@echo "Building image $(IMAGE_BASE):$(VERSION)..."
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	@echo "✓ Image built successfully"

# Test the built Docker image
test-image:
	@echo "Testing built image..."
	@docker image inspect "$(IMAGE_BASE):$(VERSION)" > /dev/null || (echo "ERROR: Image not built" && exit 1)
	@echo "✓ Image exists and is valid"

# Push image to registry
push:
	@echo "Pushing image $(IMAGE_BASE):$(VERSION) to registry..."
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"
	@echo "✓ Image pushed successfully"

docker-build:
	docker build -t eightbitsaxlounge-data .

docker-run:
	docker run -d --name eightbitsaxlounge-data -p 8080:8080 eightbitsaxlounge-data

# Help target
help:
	@echo "Available targets:"
	@echo "  lint               - Run Go linter"
	@echo "  test               - Run Go unit tests"
	@echo "  build-image        - Build Docker image locally"
	@echo "  test-image         - Verify built image exists"
	@echo "  push               - Push image to registry"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"

.PHONY: lint test build-image test-image push docker-build docker-run help