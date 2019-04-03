# Microservices Reference Implementation
Microsoft patterns & practices

This reference implementation shows a set of best practices for building and running a microservices architecture on Microsoft Azure, using Kubernetes.

## Guidance

This project has a companion set of articles that describe challenges, design patterns, and best practices for building microservices architecture. You can find these articles on the Azure Architecture Center:

- [Designing, building, and operating microservices on Azure with Kubernetes](https://docs.microsoft.com/azure/architecture/microservices)

## Scenario

​Fabrikam, Inc. (a fictional company) is starting a drone delivery service. The company manages a fleet of drone aircraft. Businesses register with the service, and users can request a drone to pick up goods for delivery. When a customer schedules a pickup, a backend system assigns a drone and notifies the user with an estimated delivery time. While the delivery is in progress, the customer can track the location of the drone, with a continuously updated ETA.

## The Drone Delivery app

The Drone Delivery application is a sample application that consists of several microservices. Because it's a sample, the functionality is simulated, but the APIs and microservices interactions are intended to reflect real-world design patterns.

- Ingestion service. Receives client requests and buffers them.
- Scheduler service. Dispatches client requests and manages the delivery workflow.
- Supervisor service. Monitors the workflow for failures and applies compensating transactions.
- Account service. Manages user accounts.
- Third-party Transportation service. Manages third-party transportation options.
- Drone service. Schedules drones and monitors drones in flight.
- Package service. Manages packages.
- Delivery service. Manages deliveries that are scheduled or in-transit.
- Delivery History service. Stores the history of completed deliveries.

![](./architecture.png)

## Test results and metrics
The Drone Delivery application has been tested up to 2000 messages/sec:


|                                          | Replicas | ~Max CPU (mc) | ~Max Mem (MB) | Avg. Throughput*        | Max. Throughput*        | Avg (ms) | 50<sup>th</sup> (ms) | 95<sup>th</sup> (ms) | 99<sup>th</sup> (ms) |
|------------------------------------------|----------|---------------|---------------|-------------------------|-------------------------|----------|-----------|-----------|-----------|
| Nginx                                    | 1        | N/A           | N/A           | serve: 1595 reqs/sec    | serve: 1923 reqs/sec    | N/A      | N/A       | N/A       | N/A       |
| Ingestion                                | 10       | 474           | 488           | ingest: 1275 msgs/sec   | ingest: 1710 msgs/sec   | 251      | 50.1      | 1560      | 2540      |
| Workflow (receive messages)              | 35       | 1445          | 79            | egress: 1275 msgs/sec   | egress: 1710 msgs/sec   | 81.5     | 0         | 25.7      | 121       |
| Workflow (call backend services + mark message as complete) | 35       | 1445          | 79            | complete: 1100 msgs/sec | complete: 1322 msgs/sec | 561.8    | 447       | 1350      | 2540      |
| Package                                  | 50       | 213           | 78            | N/A                     | N/A                     | 67.5     | 53.9      | 165       | 306       |
| Delivery                                 | 50       | 328           | 334           | N/A                     | N/A                     | 93.8     | 82.4      | 200       | 304       |
| Dronescheduler                           | 50       | 402           | 301           | N/A                     | N/A                     | 85.9     | 72.6      | 203       | 308       |



*sources:
1. Serve: Visual Studio Load Test Throughout Request/Sec
2. Ingest: Azure Service Bus metrics Incoming Messages/Sec
3. Egress: Azure Service Bus metrics Outgoing Messages/Sec
4. Complete: AI Service Bus Complete dependencies  
5. Avg/50<sup>th</sup>/95<sup>th</sup>/99<sup>th</sup>: AI dependencies
6. CPU/Mem: Azure Monitor for Containers


## Deployment

To deploy the solution, follow the steps listed [here](./deployment.md).




---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
