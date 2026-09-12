# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-db
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Validate Dockerfile and version
test:
	@echo "Running validation tests..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@echo "✓ Dockerfile exists"
	@echo "✓ version.txt exists and is valid ($(VERSION))"
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

# Help target
help:
	@echo "Available targets:"
	@echo "  test               - Validate Dockerfile and version.txt"
	@echo "  build-image        - Build Docker image locally (no push)"
	@echo "  test-image         - Verify built image exists"
	@echo "  push               - Push image to registry"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"

.PHONY: test build-image test-image push help
