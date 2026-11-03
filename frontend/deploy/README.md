# Frontend deployment

Container image and Kubernetes manifests for the Booking web app.

```
deploy/
├── docker/
│   ├── Dockerfile                # multi-stage: Node 20 build → nginx-unprivileged, non-root, port 8080
│   ├── Dockerfile.dockerignore   # used automatically by BuildKit for this Dockerfile
│   └── nginx.conf                # SPA fallback, caching, /healthz
└── kubernetes/
    ├── kustomization.yaml        # kubectl apply -k
    ├── namespace.yaml            # namespace "booking"
    ├── configmap.yaml            # nginx default.conf + env vars
    ├── deployment.yaml           # replicas, resources, probes
    ├── service.yaml              # ClusterIP :80 → 8080
    ├── ingress.yaml              # booking.local/
    └── hpa.yaml                  # 1–5 pods on CPU 70%
```

## Build the image

Run from the `frontend/` folder (the build context):

```sh
docker build -f deploy/docker/Dockerfile -t booking-frontend:latest .
docker run --rm -p 8082:8080 booking-frontend:latest   # http://localhost:8082
```

The production build uses `src/environments/environment.prod.ts`, so the app calls the API at
`/api` on the same host. `ng serve` keeps using `http://localhost:5170/api`
(`src/environments/environment.ts`).

## Deploy

Deploy the backend first (see `backend/deploy/README.md`), then:

```sh
kubectl apply -k frontend/deploy/kubernetes
kubectl -n booking rollout status deploy/booking-frontend
```

Open `http://booking.local` (mapped to your ingress IP in your hosts file).

## Configuration

`kubernetes/configmap.yaml` holds the NGINX server config (`default.conf`, mounted over the copy in
the image) and `NGINX_ENTRYPOINT_QUIET_LOGS`. Keep `default.conf` identical to
`docker/nginx.conf`. After a change:

```sh
kubectl apply -k frontend/deploy/kubernetes
kubectl -n booking rollout restart deploy/booking-frontend
```

## Sizing and scaling

| | Value |
|---|---|
| Replicas | 1 (HPA scales 1–5) |
| CPU request / limit | 50m / 200m |
| Memory request / limit | 64Mi / 128Mi |
| HPA | min 1, max 5; CPU 70%; scale-down after 300 s |
