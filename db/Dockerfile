# Use the official CouchDB image as the base image.
#
# Pinned to 3.5 plus a digest, not `latest` (CKV_DOCKER_7) — Dependabot
# (.github/dependabot.yml, docker ecosystem) moves this forward deliberately.
FROM couchdb:3.5@sha256:9ea24cbd76522fe845d1c32c7fd1dcfc8a3ba73dcc4817d62f8a7f7f1dfaffe3

# Expose the default CouchDB port
EXPOSE 5984

# Set environment variables for CouchDB configuration
ENV COUCHDB_USER=admin
ENV COUCHDB_PASSWORD=${DB_COUCHDB_PASSWORD}

# Copy initialization script
COPY init-couchdb.sh /usr/local/bin/init-couchdb.sh
RUN chmod +x /usr/local/bin/init-couchdb.sh

# CKV_DOCKER_3, verified rather than assumed (2026-09-08). The base image's
# own /docker-entrypoint.sh — which init-couchdb.sh calls — is written to
# run as root and drop to `couchdb` (uid 5984) via `setpriv` only when it
# detects `id -u = 0`; it is equally correct starting non-root, in which case
# it skips that block entirely and execs directly. Confirmed live: with this
# USER line, the container starts, single-node setup runs, and `_up` answers
# `{"status":"ok"}` — no chown-on-root step required, no functionality lost.
#
# What this does NOT solve on its own: the PVC this mounts at
# /opt/couchdb/data (db/k8s/persistentvolumeclaim.yaml) arrives as an empty,
# root-owned volume from the CSI driver — it does not inherit this image's
# baked-in couchdb:couchdb ownership, because a fresh PersistentVolume has no
# relationship to any image layer. Verified locally: uid 5984 against a
# root-owned volume gets `Permission denied`; the same uid against a volume
# whose group was set to 5984 (setgid, group-writable) writes cleanly. That
# is what `securityContext.fsGroup: 5984` does at the pod level in
# Kubernetes, and it is a platform-repo manifest requirement, not something
# this Dockerfile can satisfy — see the eightbitsaxlounge migration plan.
USER couchdb

# Use our initialization script as entrypoint
ENTRYPOINT ["/usr/local/bin/init-couchdb.sh"]
