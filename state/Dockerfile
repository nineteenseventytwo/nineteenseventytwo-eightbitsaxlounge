# nats-cli to be used for jetstream management in the entrypoint script.
# Builder stage only — never runs, so pinning here is about reproducible
# builds, not runtime hardening.
FROM natsio/nats-box:0.14.5@sha256:0784ab710aefaf6ef037ed797ee7dcde613c6ad208c4dbff1945fc7c1b5b5375 AS natscli

# state image. Pinned past the floating `2-alpine` tag to the version it
# resolves to today (CKV_DOCKER_7) — confirmed via `nats-server -v` before
# pinning, so this is not a guess.
FROM nats:2.14.6-alpine@sha256:ad7a43eb7e3337c3c38ce5d784d1461791f95f730f252d2b25eee699752a0ca3

# CKV_DOCKER_3. Unlike db's couchdb, this base ships no dedicated user at
# all — confirmed live: default id is uid=0(root), and neither /etc/passwd
# nor /etc/group has a `nats` entry. So one is created here rather than
# adopted.
RUN addgroup -g 1000 nats-run && adduser -D -u 1000 -G nats-run nats-run

# Add the nats CLI so the bootstrap script can create JetStream streams
COPY --from=natscli /usr/local/bin/nats /usr/local/bin/nats

# Entrypoint script for credential management and config generation
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

COPY jetstream-bootstrap.sh /usr/local/bin/jetstream-bootstrap.sh
RUN chmod +x /usr/local/bin/jetstream-bootstrap.sh

# entrypoint.sh writes the rendered config here at container start
# (mkdir -p /etc/nats; ... > /etc/nats/nats.conf), so nats-run needs to own
# the directory now, at build time, while it is still part of the image
# rather than a mounted volume.
COPY nats.conf.template /etc/nats/nats.conf.template
RUN chown -R nats-run:nats-run /etc/nats

# Empty placeholder, owned by nats-run — not for the real deployment (a
# PersistentVolumeClaim always mounts over this path in Kubernetes, and
# that mount arrives root-owned regardless of what's baked here, which is
# exactly why the platform-repo StatefulSet still needs
# securityContext.fsGroup: 1000 — this doesn't replace that). It's for
# `make test`'s smoke test, which runs the image with no volume mounted at
# all: confirmed live, without this the container crashes outright —
# "[FTL] Can't start JetStream: could not create storage directory - mkdir
# /data: permission denied" — since non-root has nothing to write to
# otherwise.
RUN mkdir -p /data && chown nats-run:nats-run /data

# What this does NOT solve: JetStream's store_dir is /data/jetstream
# (nats.conf.template), and /data is the PVC in
# state/k8s/statefulset.yaml's volumeClaimTemplate. Same shape as db's
# finding — a fresh PersistentVolume arrives root-owned regardless of this
# image's own user, so the platform-repo StatefulSet needs
# `securityContext.fsGroup: 1000` to match nats-run's gid.
USER nats-run

# Env vars expected:
#   SYSTEM_PASS, OVERLAY_PASS, CHAT_PASS, MIDI_PASS, DATA_PASS

EXPOSE 4222 8222 6222
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
