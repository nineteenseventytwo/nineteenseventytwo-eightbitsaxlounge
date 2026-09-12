# CI/CD Variables (set by GitHub Actions)
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE = ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-midi

# ansible-playbook isn't installed on the self-hosted runner's own image
# (ghcr.io/nineteenseventytwo/gha-runner) — deliberately minimal/hardened,
# ADR-0010 — so these targets spawn
# ghcr.io/nineteenseventytwo/ansible-runner:stable as a SIBLING container
# over the shared docker socket instead (same pattern the runner already
# uses for `docker build`/`push` above). DOCKER_GID is unset (no-op) on a
# workstation; CI sets it to the host's real docker group gid
# (stat -c %g /var/run/docker.sock) because the sibling container that
# actually reads the keys below isn't running as the uid that owns them —
# only group membership bridges that gap. Full write-up of this exact
# gotcha: nineteenseventytwo-platform's deploy-nodes.yml.
ANSIBLE_RUNNER ?= ghcr.io/nineteenseventytwo/ansible-runner:stable
DOCKER_USER = --user "$(shell id -u):$(shell id -g)" $(if $(DOCKER_GID),--group-add $(DOCKER_GID))

# hosts: master/worker playbooks (data-init, data-upload, request-seteffect)
# connect via ansible-console — the same identity and Vault-signed,
# short-lived certificate nineteenseventytwo-platform's own cluster ansible
# uses, to the same Linux nodes. On 1972-console-1 this key is
# group:docker mode:0640 for exactly this sibling-container reason; CI
# signs a fresh cert with bootstrap/ssh/sign-ci.sh before these targets run.
# ?=, not := — CI overrides this to $RUNNER_TEMP/ansible-console-cert.pub,
# since sign-ci.sh runs inside the runner container where the key's own
# directory isn't mounted, and $RUNNER_TEMP is the one path that's
# host-identical so the sibling container below can still see it.
ANSIBLE_CONSOLE_KEY  ?= /home/mchellmer/.ssh/ansible-console
ANSIBLE_CONSOLE_CERT ?= $(ANSIBLE_CONSOLE_KEY)-cert.pub

# Mounted read-only at a scratch path, then copied to $HOME/.ssh and
# chmod 600 inside the container before ansible-playbook runs — mounting
# either key straight at 0640 and relying on --group-add is not enough on
# its own. OpenSSH's private-key permission check rejects ANY group/other
# bits unconditionally (confirmed live: still rejected even when the
# invoking uid numerically matched the file's own owner, which made
# --group-add moot for that test and pointed straight at the raw mode
# bits as the actual rule). A copy is the only way to get a 600 file the
# client will accept.
ANSIBLE_CLUSTER_RUN = docker run --rm -i $(DOCKER_USER) \
	-e HOME=/home/ansible \
	-v $(CURDIR):/work -w /work \
	-v /etc/ansible:/etc/ansible:ro \
	-v $(ANSIBLE_CONSOLE_KEY):/tmp/ansible-console-key:ro \
	$(if $(wildcard $(ANSIBLE_CONSOLE_CERT)),-v $(ANSIBLE_CONSOLE_CERT):/tmp/ansible-console-cert.pub:ro,) \
	-e ANSIBLE_HOST_KEY_CHECKING=False \
	-e NAMESPACE -e UPLOAD_TYPE -e DEVICE -e DEVICE_EFFECT -e DEVICE_EFFECT_SETTING -e VALUE -e SELECTION \
	$(ANSIBLE_RUNNER) \
	sh -c 'mkdir -p "$$HOME/.ssh" && cp /tmp/ansible-console-key "$$HOME/.ssh/id_ed25519" && chmod 600 "$$HOME/.ssh/id_ed25519" && { [ -f /tmp/ansible-console-cert.pub ] && cp /tmp/ansible-console-cert.pub "$$HOME/.ssh/id_ed25519-cert.pub" || true; } && export ANSIBLE_PRIVATE_KEY_FILE="$$HOME/.ssh/id_ed25519" && exec ansible-playbook "$$@"' --

# hosts: midi-host (deploy-pc) connects to the physical Windows PC via
# pi-to-midi-host — a static key generated once on 1972-console-1 and
# authorized directly on the PC as a one-time setup step (the
# playbook that did it lived in server/, retired along with the rest of
# that layer in Phase 6.2), not a Vault-signed
# cert, since the PC isn't part of the SSH-CA-issued Linux fleet. Same
# copy-then-chmod treatment as ansible-console above, same reason. Also
# needs the Windows-specific ansible collections, which aren't in the base
# ansible-runner image (that one's built for the Linux/k8s side) —
# installed once into ANSIBLE_COLLECTIONS_DIR on the console host:
#   ansible-galaxy collection install ansible.windows community.windows \
#     -p <dir>
# Mounted at /opt/ansible-collections, not under $HOME/.ansible —
# mounting anything directly under a subdirectory of $HOME/.ansible forces
# Docker to auto-create that parent as root:root before the bind mount
# lands, which silently overrides the image's own world-writable $HOME and
# then breaks ansible's later, unrelated `mkdir $HOME/.ansible/tmp`
# (confirmed live: "Permission denied" on that exact path, gone the moment
# the collections mount moved outside $HOME entirely).
MIDI_PC_KEY             ?= /home/mchellmer/.ssh/pi-to-midi-host
ANSIBLE_COLLECTIONS_DIR ?= /home/mchellmer/ansible-home/.ansible/collections

ANSIBLE_PC_RUN = docker run --rm -i $(DOCKER_USER) \
	-e HOME=/home/ansible \
	-v $(CURDIR):/work -w /work \
	-v /etc/ansible:/etc/ansible:ro \
	-v $(MIDI_PC_KEY):/tmp/pi-to-midi-host-key:ro \
	-v $(ANSIBLE_COLLECTIONS_DIR):/opt/ansible-collections:ro \
	-e ANSIBLE_HOST_KEY_CHECKING=False \
	-e ANSIBLE_COLLECTIONS_PATH=/opt/ansible-collections:/usr/share/ansible/collections \
	-e ENVIRONMENT -e VERSION -e ARTIFACT_PATH -e NEEDS_BUILD \
	-e CONNECTIONSTRINGS__EIGHTBITSAXLOUNGEDATALAYER \
	-e AUTHENTICATION__TOKENEXPIRYINMINUTES -e AUTHENTICATION__SECRETKEY \
	-e AUTHENTICATION__ISSUER -e AUTHENTICATION__AUDIENCE \
	-e AUTHENTICATION__CLIENTS__0__CLIENTID -e AUTHENTICATION__CLIENTS__0__CLIENTSECRET \
	-e AUTHENTICATION__CLIENTS__1__CLIENTID -e AUTHENTICATION__CLIENTS__1__CLIENTSECRET \
	-e MIDIDEVICESERVICE__BYPASSKEY \
	$(if $(ARTIFACT_PATH),-v $(ARTIFACT_PATH):$(ARTIFACT_PATH):ro,) \
	$(ANSIBLE_RUNNER) \
	sh -c 'mkdir -p "$$HOME/.ssh" && cp /tmp/pi-to-midi-host-key "$$HOME/.ssh/pi-to-midi-host" && chmod 600 "$$HOME/.ssh/pi-to-midi-host" && export ANSIBLE_PRIVATE_KEY_FILE="$$HOME/.ssh/pi-to-midi-host" && exec ansible-playbook "$$@"' --

.PHONY: help lint test build-image test-image push deploy-k8s build-pc deploy-pc
.PHONY: deploy-midi-pc deploy-midi-api docker-build data-init data-upload request-seteffect

help:
	@echo "MIDI API - Build and Deployment Targets"
	@echo ""
	@echo "Standard CI/CD targets:"
	@echo "  lint              - Run .NET code formatter"
	@echo "  test              - Run .NET unit tests"
	@echo "  build-image       - Build Docker image locally (no push)"
	@echo "  test-image        - Verify built image exists"
	@echo "  push              - Push image to registry"
	@echo "  deploy-k8s        - Legacy — superseded by platform/Argo CD, left broken"
	@echo "                      intentionally (see midi/Makefile's own comment)"
	@echo "  build-pc          - Build Windows self-contained binary"
	@echo "  deploy-pc         - Deploy MIDI to Windows PC"
	@echo "  data-init         - Initialize the MIDI datamodel (devices/effects/selectors dbs)"
	@echo "  data-upload       - Upload effects or device configs. UPLOAD_TYPE=effects|devices"
	@echo "  request-seteffect - Send a SetEffect request for interactive testing"
	@echo ""
	@echo "Environment Variables:"
	@echo "  VERSION=<version>                 - Override version (default: from version.txt)"
	@echo "  NAMESPACE=<namespace>             - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER=<owner>   - Container registry owner"

# Standard CI/CD targets
lint:
	@echo "Running .NET linter..."
	@dotnet format NineteenSeventyTwo.EightBitSaxLounge.Midi.sln --verify-no-changes --verbosity diagnostic

test:
	@echo "Running .NET tests..."
	@dotnet restore NineteenSeventyTwo.EightBitSaxLounge.Midi.sln
	@dotnet build NineteenSeventyTwo.EightBitSaxLounge.Midi.sln --no-restore --configuration Release
	@dotnet test --solution NineteenSeventyTwo.EightBitSaxLounge.Midi.sln --no-build --configuration Release

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

# NOT fixed to use the sibling-container pattern deliberately: midi's own
# k8s Deployment/Service is now owned by platform/Argo CD (ADR-0012,
# nineteenseventytwo-platform#154) — fixing this to actually run again
# would race Argo CD to manage the same resources, which is actively
# harmful, not just dead code. Left broken until eightbitsaxlounge/server
# is fully retired (Phase 6.2), matching db/data/state's own release
# workflows, which coexist with their own equally-dead ansible deploy
# targets the same way.
deploy-k8s:
	@echo "Deploying MIDI API to Kubernetes namespace: $(NAMESPACE)..."
	ansible-playbook midi-api-deploy.yaml -vv

build-pc:
	@echo "Building Windows self-contained binary version $(VERSION)..."
	@mkdir -p publish
	cd NineteenSeventyTwo.EightBitSaxLounge.Midi && \
	dotnet publish NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi/NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.csproj \
		--configuration Release \
		--runtime win-x64 \
		--self-contained true \
		-p:PublishSingleFile=false \
		-p:Version=$(VERSION) \
		--output ../publish/bin
	cd publish/bin && tar -czf ../midi-api-$(VERSION).tar.gz *
	@echo "Package created: publish/midi-api-$(VERSION).tar.gz"

deploy-pc:
	@echo "Deploying MIDI to Windows PC..."
	$(ANSIBLE_PC_RUN) midi-pc-deploy.yaml -vv

deploy-midi-pc: deploy-pc

deploy-midi-api: deploy-k8s

data-init:
	$(ANSIBLE_CLUSTER_RUN) midi-data-init.yaml -vv

data-upload:
	@if [ -z "$(UPLOAD_TYPE)" ]; then \
		echo "Error: UPLOAD_TYPE not set. Use: make data-upload UPLOAD_TYPE=effects|devices"; \
		exit 1; \
	fi
	UPLOAD_TYPE=$(UPLOAD_TYPE) $(ANSIBLE_CLUSTER_RUN) midi-data-upload.yaml -vv

request-seteffect:
	@if [ -z "$(DEVICE)" ] || [ -z "$(DEVICE_EFFECT)" ] || [ -z "$(DEVICE_EFFECT_SETTING)" ]; then \
		echo "Error: Required parameters not set."; \
		echo "Usage: make request-seteffect DEVICE=VentrisDualReverb DEVICE_EFFECT=ReverbEngineA DEVICE_EFFECT_SETTING=ReverbEngine [VALUE=<int>] [SELECTION=<string>]"; \
		exit 1; \
	fi
	DEVICE=$(DEVICE) \
	DEVICE_EFFECT=$(DEVICE_EFFECT) \
	DEVICE_EFFECT_SETTING=$(DEVICE_EFFECT_SETTING) \
	VALUE=$(VALUE) \
	SELECTION=$(SELECTION) \
	$(ANSIBLE_CLUSTER_RUN) midi-request-seteffect.yaml -vv

docker-build:
	docker build -t eightbitsaxlounge-midi .
