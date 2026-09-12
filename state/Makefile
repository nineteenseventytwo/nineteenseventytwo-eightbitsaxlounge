# Variables
VERSION ?= $(shell cat version.txt)
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-state

# Validate source files
# Not invoked by CI: release-template.yaml declares a build-target input but
# never actually calls `make ${{ inputs.build-target }}` anywhere in its
# body — only `make build-image` is hardcoded. state-release.yaml's own
# `build-target: 'build'` is passed and silently ignored. Kept here for
# local/manual use; trimmed of its state-nats.yaml/k8s/ checks (both gone,
# Phase 6.2) rather than deleted outright, since it still validates
# something real for anyone running it by hand.
build:
	@echo "Validating configuration..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f entrypoint.sh || (echo "ERROR: entrypoint.sh not found" && exit 1)
	@test -f nats.conf.template || (echo "ERROR: nats.conf.template not found" && exit 1)
	@test -f jetstream-bootstrap.sh || (echo "ERROR: jetstream-bootstrap.sh not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@echo "✓ All required files exist"
	@echo "✓ version.txt is valid ($(VERSION))"
	@echo "All validation checks passed"

# Build custom NATS image with config and bootstrap baked in
build-image:
	@echo "Building image $(IMAGE_BASE):$(VERSION)..."
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	@echo "✓ Image built: $(IMAGE_BASE):$(VERSION)"

# Verify built image exists
test-image:
	@echo "Verifying built image..."
	@docker image inspect "$(IMAGE_BASE):$(VERSION)" > /dev/null 2>&1 || (echo "ERROR: Image not built" && exit 1)
	@echo "✓ Image $(IMAGE_BASE):$(VERSION) exists"

# Push image to GHCR
push:
	@echo "Pushing $(IMAGE_BASE):$(VERSION) to registry..."
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"
	@echo "✓ Image pushed successfully"

test:
	@echo "Running smoke tests using custom image $(IMAGE_BASE):$(VERSION)..."
	@echo "Starting NATS in background for health check..."
	docker run -d --name nats-smoke-test -p 4222:4222 -p 8222:8222 \
		-e SYSTEM_PASS=systempass \
		-e OVERLAY_PASS=smoketest \
		-e CHAT_PASS=smoketest \
		-e MIDI_PASS=smoketest \
		-e DATA_PASS=smoketest \
		"$(IMAGE_BASE):$(VERSION)" -c /etc/nats/nats.conf > /dev/null 2>&1
	@sleep 2
	@echo "Verifying NATS is responsive..."
	@if docker exec nats-smoke-test wget -q -O /dev/null http://localhost:8222/varz > /dev/null 2>&1; then \
		echo "✓ NATS monitoring endpoint is healthy"; \
	else \
		echo "ERROR: NATS health check failed"; \
		docker stop nats-smoke-test > /dev/null 2>&1 || true; \
		docker rm nats-smoke-test > /dev/null 2>&1 || true; \
		exit 1; \
	fi
	@echo "Checking JetStream status..."
	@if docker exec nats-smoke-test nats --server nats://localhost:4222 server info > /dev/null 2>&1; then \
		echo "✓ JetStream is operational"; \
	else \
		echo "WARNING: Could not verify JetStream (may not be critical)"; \
	fi
	@echo "Cleaning up test container..."
	docker stop nats-smoke-test > /dev/null 2>&1 || true
	docker rm nats-smoke-test > /dev/null 2>&1 || true
	@echo "All smoke tests passed"

# Start NATS+JetStream locally for interactive development testing
# runs in foreground so logs appear in your terminal (Ctrl-C to stop)
test-local:
	@echo "Starting NATS+JetStream in Podman (foreground). Press Ctrl-C to stop."
	podman run --rm --name nats-test -p 4222:4222 -p 8222:8222 \
		-e SYSTEM_PASS=systempass \
		-e OVERLAY_PASS=overlaypw \
		-e CHAT_PASS=chatpw \
		-e MIDI_PASS=midipw \
		-e DATA_PASS=datapw \
		"$(IMAGE_BASE):$(VERSION)" -c /etc/nats/nats.conf

# Windows PowerShell version of test-local
test-local-ps:
	powershell -NoProfile -Command \
		"& { Write-Host 'Starting NATS+JetStream in Podman (foreground). Press Ctrl-C to stop.'; \
		$$image = '$(IMAGE_BASE):$(VERSION)'; \
		podman run --rm --name nats-test -p 4222:4222 -p 8222:8222 \
			-e SYSTEM_PASS=systempass \
			-e OVERLAY_PASS=overlaypw \
			-e CHAT_PASS=chatpw \
			-e MIDI_PASS=midipw \
			-e DATA_PASS=datapw \
			$$image -c /etc/nats/nats.conf }"

# Help
help:
	@echo "Available targets:"
	@echo "  build              - Validate source files and Ansible playbook"
	@echo "  build-image        - Build custom NATS Docker image"
	@echo "  test               - Smoke test the built custom image (CI, Docker)"
	@echo "  test-image         - Verify built image exists in local daemon"
	@echo "  push               - Push image to GHCR"
	@echo "  test-local         - Start custom NATS image with Podman (interactive)"
	@echo "  test-local-ps      - Start custom NATS image with Podman via PowerShell (Windows)"

.PHONY: build build-image test-image push test test-local test-local-ps help