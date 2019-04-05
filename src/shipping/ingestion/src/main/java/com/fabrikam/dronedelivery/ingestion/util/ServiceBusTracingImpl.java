// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion.util;

import java.util.Locale;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.function.Function;

import com.microsoft.applicationinsights.TelemetryClient;
import com.microsoft.applicationinsights.extensibility.context.OperationContext;
import com.microsoft.applicationinsights.telemetry.Duration;
import com.microsoft.applicationinsights.telemetry.RemoteDependencyTelemetry;
import com.microsoft.applicationinsights.telemetry.RequestTelemetry;
import com.microsoft.applicationinsights.web.internal.ThreadContext;
import com.microsoft.applicationinsights.web.internal.correlation.TelemetryCorrelationUtils;
import com.microsoft.azure.servicebus.IMessage;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

@Component
public class ServiceBusTracingImpl implements ServiceBusTracing {
	private static final long NANO_TO_MILI_SECONDS = TimeUnit.MILLISECONDS.toNanos(1);

	private static final String DIAGNOSTIC_ID_PROPERTY_NAME = "Diagnostic-Id";

	private static final String SERVICE_BUS_REMOTE_DEPENDENCY_TYPE = "Azure Service Bus";

	private static final String SERVICE_BUS_REMOTE_DEPENDENDENCY_NAME = "Send";

	private static final String TARGET_REMOTE_DEPENDENDENCY_FORMAT = "%s | %s";

	private final TelemetryClient telemetryClient;

	@Autowired
	public ServiceBusTracingImpl(TelemetryClient telemetryClient)
	{
		this.telemetryClient = telemetryClient;
	}

	@Override
	public CompletableFuture<Void> trackAndCorrelateServiceBusDependency(
		String endpoint,
		String queueName,
		IMessage message,
		Function<IMessage, CompletableFuture<Void>> func) {

		CompletableFuture<Void> result;

		String target = String.format(
							Locale.US,
							TARGET_REMOTE_DEPENDENDENCY_FORMAT,
							endpoint,
							queueName);

		propagateCorrelationProperties(message);

		final RemoteDependencyTelemetry remoteDependency = createRemoteDependencyTelemetry(
			target);

		final long start = System.nanoTime();
		result = func.apply(message);
		result.whenComplete((r,t) -> {
			final long finish = System.nanoTime();
			final long intervalMs = (finish - start) / NANO_TO_MILI_SECONDS;
			boolean successful = false;
			if (t == null) {
				successful = true;
			}

			remoteDependency.setDuration(new Duration(intervalMs));
			remoteDependency.setSuccess(successful);

			telemetryClient.trackDependency(remoteDependency);

			if(successful == false){
				RuntimeException runtimeEx = new RuntimeException(t);
				telemetryClient.trackException(runtimeEx);
			}
		});

		return result;
	}

	private static void propagateCorrelationProperties(IMessage message) {
		String parentId = ThreadContext
							.getRequestTelemetryContext()
							.getHttpRequestTelemetry()
							.getId();

		// propagate Service Bus required properties
		// https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-end-to-end-tracing
		// https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation#telemetry-correlation-in-the-java-sdk
		message.getProperties().put(DIAGNOSTIC_ID_PROPERTY_NAME, parentId);
	}

	private static RemoteDependencyTelemetry createRemoteDependencyTelemetry(
		String target){

		return createRemoteDependencyTelemetry(target, new Duration(0L), true);
	}

	private static RemoteDependencyTelemetry createRemoteDependencyTelemetry(
		String target,
		Duration duration,
		boolean successful) {
		String dependencyId = TelemetryCorrelationUtils
								.generateChildDependencyId();

		RemoteDependencyTelemetry dependencyTelemetry =
			new RemoteDependencyTelemetry(
				SERVICE_BUS_REMOTE_DEPENDENDENCY_NAME,
				"",
				duration,
				successful);

		dependencyTelemetry.setId(dependencyId);
		dependencyTelemetry.setType(SERVICE_BUS_REMOTE_DEPENDENCY_TYPE);
		dependencyTelemetry.setTarget(target);

		RequestTelemetry requestTelemetry = ThreadContext
												.getRequestTelemetryContext()
												.getHttpRequestTelemetry();

		String parentId = requestTelemetry.getId();
		String operationId = extractRootId(parentId);

		OperationContext operationContext = dependencyTelemetry
												.getContext()
												.getOperation();
		operationContext.setParentId(parentId);
		operationContext.setId(operationId);

		return dependencyTelemetry;
	}

	private static String extractRootId(String parentId) {
		int rootEnd = parentId.indexOf('.');
		if (rootEnd < 0) {
		  rootEnd = parentId.length();
		}

		int rootStart = parentId.charAt(0) == '|' ? 1 : 0;
		return parentId.substring(rootStart, rootEnd);
	  }
}
