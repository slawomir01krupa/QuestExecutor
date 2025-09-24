# QuestExecutor

QuestExecutor is a modular .NET 8 API for orchestrating and executing HTTP and PowerShell requests, with built-in validation, metrics, and resilience.  
This repository includes solution items, a ready-to-use Postman collection, and Docker support for easy local development.

---

## Solution Structure


## API Overview

- **/ping**  
  Health check endpoint.

- **/metrics**  
  Exposes application metrics.

- **/api/{**path}**  
  Orchestrator endpoint for GET, POST, PUT, PATCH, DELETE.  
  Requires headers:  
    - `X-Target-Base`
    - `X-Correlation-Id`
    - `X-Executor-Type`

---

## Postman Collection

A ready-to-use Postman collection is included at:  
`SolutionItems/QuestExecutor API.postman_collection.json`

**How to use:**
1. Open Postman.
2. Import the collection file.
3. Set the `baseUrl` variable (e.g., `https://localhost:5001`).
4. Use the provided requests to test all endpoints.

---

---

## Running Locally with Docker

**Requirements:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
 The API will be available at [https://localhost:5001](https://localhost:5001) (adjust port as needed).

---
