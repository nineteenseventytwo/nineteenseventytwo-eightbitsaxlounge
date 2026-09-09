# Variables
VERSION ?= $(shell cat version.txt)
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-state

# Validate source files and Ansible playbook
build:
	@echo "Validating configuration..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f entrypoint.sh || (echo "ERROR: entrypoint.sh not found" && exit 1)
	@test -f nats.conf.template || (echo "ERROR: nats.conf.template not found" && exit 1)
	@test -f jetstream-bootstrap.sh || (echo "ERROR: jetstream-bootstrap.sh not found" && exit 1)
	@test -f state-nats.yaml || (echo "ERROR: state-nats.yaml not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@test -f k8s/service-headless.yaml || (echo "ERROR: k8s/service-headless.yaml not found" && exit 1)
	@test -f k8s/statefulset.yaml || (echo "ERROR: k8s/statefulset.yaml not found" && exit 1)
	@test -f k8s/service-client.yaml || (echo "ERROR: k8s/service-client.yaml not found" && exit 1)
	@echo "✓ All required files exist"
	@echo "✓ version.txt is valid ($(VERSION))"
	@ansible-playbook --syntax-check state-nats.yaml > /dev/null || (echo "ERROR: Ansible syntax check failed" && exit 1)
	@echo "✓ Ansible playbook syntax is valid"
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

# Deploy NATS to Kubernetes cluster
deploy:
	@test -n "$$NAMESPACE" || (echo "ERROR: NAMESPACE env var not set" && exit 1)
	@test -n "$$SYSTEM_PASS" || (echo "ERROR: SYSTEM_PASS env var not set" && exit 1)
	@test -n "$$OVERLAY_PASS" || (echo "ERROR: OVERLAY_PASS env var not set" && exit 1)
	@test -n "$$CHAT_PASS" || (echo "ERROR: CHAT_PASS env var not set" && exit 1)
	@test -n "$$MIDI_PASS" || (echo "ERROR: MIDI_PASS env var not set" && exit 1)
	@test -n "$$DATA_PASS" || (echo "ERROR: DATA_PASS env var not set" && exit 1)
	@echo "🚀 Deploying NATS to namespace $$NAMESPACE with version $(VERSION)"
	NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
		SYSTEM_PASS="$$SYSTEM_PASS" OVERLAY_PASS="$$OVERLAY_PASS" CHAT_PASS="$$CHAT_PASS" MIDI_PASS="$$MIDI_PASS" DATA_PASS="$$DATA_PASS" \
		ansible-playbook state-nats.yaml

# Manual deployment with interactive prompts for secrets
deploy-nats-manual:
	@if [ -z "$$NAMESPACE" ] || [ -z "$$SYSTEM_PASS" ] || [ -z "$$OVERLAY_PASS" ] || [ -z "$$CHAT_PASS" ] || [ -z "$$MIDI_PASS" ] || [ -z "$$DATA_PASS" ]; then \
		echo "⚠️  Manual deployment mode: using prompts for secrets"; \
		read -p "Enter NAMESPACE (default: eightbitsaxlounge-dev): " ns; \
		NAMESPACE="$${ns:-eightbitsaxlounge-dev}"; \
		read -sp "Enter SYSTEM_PASS: " system_pass; echo; \
		read -sp "Enter OVERLAY_PASS: " overlay_pass; echo; \
		read -sp "Enter CHAT_PASS: " chat_pass; echo; \
		read -sp "Enter MIDI_PASS: " midi_pass; echo; \
		read -sp "Enter DATA_PASS: " data_pass; echo; \
		NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
			SYSTEM_PASS="$$system_pass" OVERLAY_PASS="$$overlay_pass" CHAT_PASS="$$chat_pass" MIDI_PASS="$$midi_pass" DATA_PASS="$$data_pass" \
			ansible-playbook state-nats.yaml; \
	else \
		NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
			SYSTEM_PASS="$$SYSTEM_PASS" OVERLAY_PASS="$$OVERLAY_PASS" CHAT_PASS="$$CHAT_PASS" MIDI_PASS="$$MIDI_PASS" DATA_PASS="$$DATA_PASS" \
			ansible-playbook state-nats.yaml; \
	fi

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
	@echo "  deploy             - Deploy NATS to Kubernetes (requires env vars)"
	@echo "  deploy-nats-manual - Deploy NATS with interactive prompts for secrets"
	@echo ""
	@echo "Environment variables for deploy:"
	@echo "  NAMESPACE          - K8s namespace (required)"
	@echo "  OVERLAY_PASS       - Password for overlay user (required)"
	@echo "  CHAT_PASS            - Password for chat user (required)"
	@echo "  MIDI_PASS          - Password for midi user (required)"
	@echo "  DATA_PASS          - Password for data user (required)"

.PHONY: build build-image test-image push test test-local test-local-ps deploy deploy-nats-manual help