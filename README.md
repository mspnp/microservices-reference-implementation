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

## Deployment

To deploy the solution, follow the steps listed [here](./deployment.md) to get deep understanding on the infrastructure you are going to create or just go for a [quick start using the ARM tempplates](./deploymentF5.md).




---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
