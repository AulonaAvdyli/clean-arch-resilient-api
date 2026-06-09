# Solution

## Overview of the Application

This application is built to be **reliable, efficient, and scalable**. It works with external services while keeping everything running smoothly. I followed **clean architecture**, used **Polly** to handle errors and retries, **Flyway** for database migrations, and **Hangfire** to run background tasks.

I also added best practices like **IP geolocation** using IPAPI, **circuit breakers**, **retry policies**, and **rate-limiting** to make sure external services don’t affect overall performance.

---

## Design and Architecture Choices

### Clean Architecture

Organized the code into three layers: **Application**, **Infrastructure**, and **Domain**. This helps:

1. Keep the main logic (business logic) separate from external systems.
2. Make it easier to switch services or databases later if needed.
3. Allow each part of the system to be tested on its own.

### Polly for Resilience

Used **Polly** to manage temporary failures when calling external services:

- **Retry Policy**: If a request fails, the app tries again up to 2 times using exponential backoff and jitter.
- **Circuit Breaker**: If a service fails 3 times in a row, requests stop for a while to let the service recover.
- **Fallback**: If all retries fail, the app returns a default response instead of crashing.

### Jitter in Retry Policies

I added **jitter** (a random delay) to retry attempts to avoid multiple retries hitting a service at the same time. This prevents **retry storms**.

---

## Key Functional Components

### IPAPI Integration

**IPAPI** to get location data based on a user's IP address. It’s built as a **separate service** to make things more flexible and scalable:

- **Separation of concerns**: The logic is kept in one place, which makes it easier to update or change.
- **Scalability**: I can switch to another provider anytime without affecting the rest of the system.
- **Caching**: I use **Redis** to cache location data so I don’t need to make API calls every time, which improves performance.

### Database Migrations with Flyway

I used **Flyway** to manage database migrations. It helps version and apply changes to the database in a controlled way. This keeps the database in sync with the codebase across different environments.

---

## Why I Made These Choices

### Clean Architecture

Clean architecture keeps things clean and organized. It also makes the app easier to maintain and grow over time without messing up the core logic.

### Polly for Resilience

External services can fail sometimes. Polly helps the app deal with that by retrying requests, handling timeouts, and falling back to safe defaults so users don’t notice any issues.

### IP Geolocation as a Service

By keeping geolocation as a separate service, I made the app more modular. This approach allows easy replacement or modification of the geolocation service without affecting other areas of the application. 
Since the app is currently running in a local environment, I’ve handled local IP addresses (like 127.0.0.1 or ::1) to be simulated as if they’re coming from Greece. 
This ensures that testing the geolocation service works seamlessly in a local development setting without requiring real external IP lookups.

### Hangfire for Background Jobs

I used Hangfire to handle background jobs, specifically for batch processing and bulk insertion. These jobs are processed asynchronously, ensuring that the main application remains fast and responsive. 
The background tasks are queued and processed in batches, which improves efficiency for large data operations. You can access the Hangfire dashboard at the following URL:
http://localhost:5000/hangfire

### Circuit Breaker

The **circuit breaker** helps avoid calling a service that’s already down. It pauses for a bit after several failures, then tries again once things seem stable.

---

## API Design & Security

### Rate Limiting

Added **rate limiting** using Polly to prevent too many requests from hitting external services. If a service starts returning a `429 Too Many Requests` error, the app automatically backs off.

### Authentication & Authorization

I used **JWT tokens** for secure login. Only logged-in users can access certain data. There are two roles for testing:

- **Developer**
  - Username: `dev`
  - Password: `dev`
- **User**
  - Username: `user`
  - Password: `user`

---

## Test Coverage

Since I followed clean architecture, writing tests was simple. I used **xUnit** for unit tests. I made sure the **service** and **repository** layers are fully tested, including the **retry** and **circuit breaker** logic. This helps make sure the app can handle failures without breaking.
Additionally, integration testing was used only for ensuring that the application starts successfully. This test checks if the application initializes without throwing any exceptions.

---

## Conclusion

This application is built with **resilience**, **scalability**, and **maintainability** in mind. I used clean architecture to keep things organized, Polly to handle errors, Flyway to manage the database, and Hangfire for background tasks.

The IP geolocation service is modular and easy to change. With **retry policies**, **circuit breakers**, and **fallbacks**, the app is designed to keep working even when external services don’t.

---

## Notes

- **Swagger UI**: API docs are available at `http://localhost:5000/swagger`.
- **Port**: The app runs locally on **port 5000**.
- **Testing**: I focused on testing all the unit parts that can be tested using Unit Testing.
- **API Docs**: All endpoints are documented with **Swagger/OpenAPI**.
