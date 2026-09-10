# CI/CD Variables (set by GitHub Actions)
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE = ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-midi

.PHONY: help lint test build-image test-image push deploy-k8s build-pc deploy-pc
.PHONY: deploy-midi-pc deploy-midi-api docker-build data-init data-upload request-seteffect

help:
	@echo "MIDI API - Build and Deployment Targets"
	@echo ""
	@echo "Standard CI/CD targets:"
	@echo "  lint             - Run .NET code formatter"
	@echo "  test             - Run .NET unit tests"
	@echo "  build-image      - Build Docker image locally (no push)"
	@echo "  test-image       - Verify built image exists"
	@echo "  push             - Push image to registry"
	@echo "  deploy-k8s       - Deploy MIDI API to Kubernetes"
	@echo "  build-pc         - Build Windows self-contained binary"
	@echo "  deploy-pc        - Deploy MIDI to Windows PC"
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
	ansible-playbook midi-pc-deploy.yaml -vv

deploy-midi-pc: deploy-pc

deploy-midi-api: deploy-k8s

data-init:
	ansible-playbook midi-data-init.yaml -vv

data-upload:
	@if [ -z "$(UPLOAD_TYPE)" ]; then \
		echo "Error: UPLOAD_TYPE not set. Use: make data-upload UPLOAD_TYPE=effects|devices"; \
		exit 1; \
	fi
	UPLOAD_TYPE=$(UPLOAD_TYPE) ansible-playbook midi-data-upload.yaml -vv

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
	ansible-playbook midi-request-seteffect.yaml -vv

docker-build:
	docker build -t eightbitsaxlounge-midi .
