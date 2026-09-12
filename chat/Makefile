# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-chat
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Lint Python code
lint:
	@echo "Running Python linter..."
	pip3 install ruff==0.16.6 --break-system-packages || pip3 install ruff==0.16.6
	ruff check src/ tests/

# Run Python tests with coverage
# Runs in an isolated python:3.14-slim container, not on the runner's own
# host: the self-hosted runner's image doesn't ship pip at all (confirmed
# live — not even `python3 -m pip` — a casualty of the same
# minimal/hardened image that also dropped ansible-playbook, see
# nineteenseventytwo-eightbitsaxlounge#75's Makefile comments in midi/).
# Never exercised before because no chat release had run since the org
# transfer. Same pattern overlay/Makefile already uses for its own tests.
test:
	@echo "Running tests in Docker (python:3.14-slim)..."
	docker run --rm \
		--user $$(id -u):$$(id -g) \
		-v "$(CURDIR):/app" \
		-w /app \
		-e HOME=/tmp \
		python:3.14-slim \
		bash -c "pip install --no-cache-dir -r requirements.txt && python -m pytest tests/ --cov=src --cov-report=xml --cov-report=term --junitxml=test-results.xml"
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

# Local development targets
install:
	pip3 install -r requirements.txt

install-dev:
	pip3 install -r requirements.txt
	pip3 install -r requirements-dev.txt

dev:
	python3 src/main.py

test-local: install-dev
	python3 -m pytest tests/ -v

clean:
	rm -rf .pytest_cache/
	rm -rf htmlcov/
	rm -rf .coverage
	rm -f test-results.xml
	rm -rf __pycache__/
	find . -name "*.pyc" -delete
	find . -name "*__pycache__*" -type d -exec rm -rf {} + 2>/dev/null || true

docker-build:
	docker build -t eightbitsaxlounge-chat:local .

docker-run:
	docker run --rm -it --env-file .env eightbitsaxlounge-chat:local

set-environment:
	ansible-playbook chat-set-environment.yaml

# Help target
help:
	@echo "Available targets:"
	@echo "  lint               - Run Python linter"
	@echo "  test               - Run tests with coverage (for CI)"
	@echo "  build-image        - Build Docker image locally"
	@echo "  test-image         - Verify built image exists"
	@echo "  push               - Push image to registry"
	@echo ""
	@echo "Local development:"
	@echo "  install            - Install Python dependencies"
	@echo "  install-dev        - Install dev dependencies"
	@echo "  dev                - Run bot locally"
	@echo "  test-local         - Run tests (verbose)"
	@echo "  clean              - Clean test artifacts"
	@echo "  docker-build       - Build Docker image locally"
	@echo "  docker-run         - Run Docker image locally"
	@echo "  set-environment    - Switch active environment (dev/prod)"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"

.PHONY: lint test build-image test-image push install install-dev dev test-local clean docker-build docker-run set-environment help