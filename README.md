# Booking System

A booking system where employees can reserve desks, office spaces, or parking spots from a single application.

## Technology Stack

| Layer        | Technology                      |
| ------------ | ------------------------------- |
| **Backend**  | .NET 8 (API, EF Core, Identity) |
| **Frontend** | Angular 19                      |
| **Database** | SQLite                          |

## Data Structure

![Data Structure](data-structure.png)

## Project Structure

```
booking-repo/
├── backend/    # .NET 8 API (see backend/README.md)
├── frontend/   # Angular app (see frontend/README.md)
└── booking-app-seed-data.json
```

Refer to each project's README for setup instructions, prerequisites, and available commands:

- [Backend README](backend/README.md)
- [Frontend README](frontend/README.md)

## Deployment

Each part has its own Docker and Kubernetes files under `deploy/`:

- [Backend deployment](backend/deploy/README.md) — API image, ConfigMap, Secret, Deployment, Service, Ingress (`/api`), HPA
- [Frontend deployment](frontend/deploy/README.md) — web image (nginx), ConfigMap, Deployment, Service, Ingress (`/`), HPA

Both are served on one host (`booking.local`): `/api` goes to the backend, everything else to the frontend.
