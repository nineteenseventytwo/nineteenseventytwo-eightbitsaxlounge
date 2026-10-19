# Build stage
FROM golang:1.27-alpine@sha256:8a5910f31396cd4d89662f56c68b3ae31d374308270a1c3bd96672ee5ed43414 AS builder
WORKDIR /app
COPY . .
RUN go build -o api

# Runtime stage
# Pinned to a digest, not `latest` — CKV_DOCKER_7. A floating tag means the
# image this Dockerfile produces today is not the image it produces next
# month, and Dependabot (docker ecosystem, .github/dependabot.yml) is what
# now moves this pin forward deliberately instead of silently.
FROM alpine:3.24@sha256:294b683cb724975bec92580e1e685676bd4b50bda910ddb8c51d4cabeaec77e6
RUN addgroup -g 1000 app && adduser -D -u 1000 -G app app
WORKDIR /app
COPY --from=builder /app/api .
# CKV_DOCKER_3. The prod tenant namespace enforces Pod Security Standards
# `restricted`, which requires runAsNonRoot — this is that, at the image
# rather than the manifest.
USER app
EXPOSE 8080
CMD ["./api"]
