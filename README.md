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


## Testing Powershell Executor 

Make sure that VM is running locally 

**On the VM use port forwarding**
How to Install and Start OpenSSH Server on Windows
1. Install OpenSSH Server
Open PowerShell as Administrator and run:

Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

2. Start the SSH Server
After installation, start the service:

Start-Service sshd

3. Set SSH Server to Start Automatically
(Optional, but recommended)

Set-Service -Name sshd -StartupType 'Automatic'

4. Allow SSH Through Windows Firewall
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22

5. Verify the Service is Running
Get-Service sshd



