# Build stage
FROM golang:1.27-alpine@sha256:d200d2dd76b03a99b52651fd1325aff7dcd35422c35c01bac56571b0507f3deb AS builder
WORKDIR /app
COPY . .
RUN go build -o api

# Runtime stage
# Pinned to a digest, not `latest` — CKV_DOCKER_7. A floating tag means the
# image this Dockerfile produces today is not the image it produces next
# month, and Dependabot (docker ecosystem, .github/dependabot.yml) is what
# now moves this pin forward deliberately instead of silently.
FROM alpine:3.24@sha256:be2656061b536c1883c4034790e21d0a09c5aaaf51968b89368a3c0371ea2c9a
RUN addgroup -g 1000 app && adduser -D -u 1000 -G app app
WORKDIR /app
COPY --from=builder /app/api .
# CKV_DOCKER_3. The prod tenant namespace enforces Pod Security Standards
# `restricted`, which requires runAsNonRoot — this is that, at the image
# rather than the manifest.
USER app
EXPOSE 8080
CMD ["./api"]
