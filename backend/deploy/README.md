# Backend deployment

Container image and Kubernetes manifests for the Booking API.

```
deploy/
├── docker/
│   ├── Dockerfile                # multi-stage: .NET 8 SDK build → ASP.NET 8 runtime, non-root, port 8080
│   └── Dockerfile.dockerignore   # used automatically by BuildKit for this Dockerfile
└── kubernetes/
    ├── kustomization.yaml        # kubectl apply -k
    ├── namespace.yaml            # namespace "booking"
    ├── configmap.yaml            # environment variables
    ├── secret.example.yaml       # example only — not applied
    ├── deployment.yaml           # replicas, resources, probes, /data volume
    ├── service.yaml              # ClusterIP :80 → 8080
    ├── ingress.yaml              # booking.local/api, cookie session affinity
    └── hpa.yaml                  # 1–5 pods on CPU 70% / memory 80%
```

## Build the image

Run from the `backend/` folder (the build context):

```sh
docker build -f deploy/docker/Dockerfile -t booking-backend:latest .
```

Try it locally:

```sh
docker run --rm -p 8081:8080 -e Jwt__Key="$(openssl rand -hex 32)" booking-backend:latest
curl http://localhost:8081/healthz        # Healthy
```

## Deploy

Prerequisites: an NGINX Ingress controller, metrics-server (for the HPA), and the image available
to the cluster (push it to a registry and set `images:` in `kustomization.yaml`, or on minikube
`minikube image load booking-backend:latest`).

```sh
kubectl create namespace booking --dry-run=client -o yaml | kubectl apply -f -
kubectl -n booking create secret generic booking-backend-secret \
  --from-literal=Jwt__Key="$(openssl rand -hex 32)"
kubectl apply -k backend/deploy/kubernetes
kubectl -n booking rollout status deploy/booking-backend
```

Map `booking.local` to your ingress IP in your hosts file. The API is at `http://booking.local/api/...`.
Swagger is not exposed through the Ingress; use
`kubectl -n booking port-forward deploy/booking-backend 8081:8080` and open
`http://localhost:8081/swagger`.

## Configuration

Set in `kubernetes/configmap.yaml` (`__` means `:` in ASP.NET Core config). After a change, re-apply
and run `kubectl -n booking rollout restart deploy/booking-backend`.

| Variable | Value in cluster | Notes |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | |
| `ConnectionStrings__DefaultConnection` | `Data Source=/data/app.db` | Must be on the writable `/data` volume |
| `Jwt__Issuer` / `Jwt__Audience` | `booking` / `booking_client` | |
| `Jwt__Key` | from Secret `booking-backend-secret` | At least 32 bytes, or the app refuses to start |
| `Cors__AllowedOrigins__0` | `http://booking.local` | Add `__1`, `__2`… for more origins |
| `Logging__LogLevel__Default` | `Information` | |

## Sizing and scaling

| | Value |
|---|---|
| Replicas | 1 (HPA scales 1–5) |
| CPU request / limit | 250m / 500m |
| Memory request / limit | 256Mi / 512Mi |
| HPA | min 1, max 5; CPU 70%, memory 80%; scale-down after 300 s |

## Known limitation: one database per pod

The API uses SQLite. Each pod keeps its own `app.db` on an `emptyDir` volume, created and seeded
when the pod starts. **Data is not shared between pods and is lost when a pod restarts.** The
Ingress uses a session-affinity cookie (`booking-affinity`) so a user keeps hitting the same pod.
This is fine for demo/dev clusters; a shared database server would be needed for production.
