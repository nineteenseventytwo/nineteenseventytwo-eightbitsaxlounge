# Build stage
FROM golang:1.27-alpine@sha256:eb61c3873c97416fff0116553fff65d67a3549f72e11d9758849a689cdd8b523 AS builder
WORKDIR /app
COPY . .
RUN go build -o api

# Runtime stage
# Pinned to a digest, not `latest` — CKV_DOCKER_7. A floating tag means the
# image this Dockerfile produces today is not the image it produces next
# month, and Dependabot (docker ecosystem, .github/dependabot.yml) is what
# now moves this pin forward deliberately instead of silently.
FROM alpine:3.24@sha256:f59abe656b692d901e184be3445e084786602108599d2a51d229c0f5a48c71bf
# apk upgrade: Alpine's zlib fix (CVE-2026-85091) was in the repository
# before this base was rebuilt with it. Release builds have no layer cache,
# so every release picks up whatever Alpine has fixed since.
RUN apk upgrade --no-cache
RUN addgroup -g 1000 app && adduser -D -u 1000 -G app app
WORKDIR /app
COPY --from=builder /app/api .
# CKV_DOCKER_3. The prod tenant namespace enforces Pod Security Standards
# `restricted`, which requires runAsNonRoot — this is that, at the image
# rather than the manifest.
USER app
EXPOSE 8080
CMD ["./api"]
