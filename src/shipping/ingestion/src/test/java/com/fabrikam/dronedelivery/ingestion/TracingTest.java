// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion;

import com.fabrikam.dronedelivery.ingestion.util.ServiceBusTracingImpl;
import com.microsoft.applicationinsights.TelemetryClient;
import com.microsoft.applicationinsights.telemetry.RemoteDependencyTelemetry;
import com.microsoft.applicationinsights.telemetry.RequestTelemetry;
import com.microsoft.applicationinsights.web.internal.RequestTelemetryContext;
import com.microsoft.applicationinsights.web.internal.ThreadContext;
import com.microsoft.azure.servicebus.IMessage;

import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.invocation.Invocation;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;
import static org.mockito.Mockito.*;

import java.util.Map;
import java.util.Collection;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionException;
import java.util.function.Function;


public class TracingTest {
	private static final String TRACK_DEPENDENCY_METHOD_NAME = "trackDependency";

	private static final int DELAY_COMPLETION_MS = 100;

	private static final String SB_QUEUE_NAME = "sbQueueName";

	private static final String SB_ENDPOINT = "sbEndpoint";

	@Rule
	public ExpectedException thrown = ExpectedException.none();

	private @Mock IMessage messageMock;

	private @Mock CompletableFuture<Void> futureMock;

	private @Mock Function<IMessage, CompletableFuture<Void>> functionMock;

	private @Mock Map<String, String> mapMock;

	private @Mock TelemetryClient telemetryClientMock;

	private @Mock RequestTelemetryContext telemetryContextMock;

	private @InjectMocks ServiceBusTracingImpl tracing;

	private RequestTelemetry requestTelemetryFake;

	@Before
	public void setUp() throws Exception {

		MockitoAnnotations.initMocks(this);
		when(messageMock.getProperties()).thenReturn(mapMock);

		requestTelemetryFake = new RequestTelemetry();
		requestTelemetryFake.setId("42");

		when(telemetryContextMock.getHttpRequestTelemetry()).thenReturn(requestTelemetryFake);
	}

	@Test
	public void trackAndCorrelate_ThenPropagateDiagnoticId() throws Exception {

		// Arrange
		ThreadContext.setRequestTelemetryContext(telemetryContextMock);
		when(functionMock.apply(eq(messageMock))).thenReturn(futureMock);

		// Act
		tracing.trackAndCorrelateServiceBusDependency(SB_ENDPOINT, SB_QUEUE_NAME, messageMock, functionMock);

		// Assert
		verify(messageMock, times(1)).getProperties();
		// TODO: validate proper Request-Id format
		verify(mapMock, times(1)).put(eq("Diagnostic-Id"), eq("42"));
	}

	@Test(timeout = DELAY_COMPLETION_MS * 10)
	public void trackAndCorrelate_ThenApplyFunctionAndTrackDependency() throws Exception {

		// Arrange
		ThreadContext.setRequestTelemetryContext(telemetryContextMock);
		CompletableFuture<Void> result = new CompletableFuture<>();
		ArgumentCaptor<RemoteDependencyTelemetry> argument = ArgumentCaptor.forClass(RemoteDependencyTelemetry.class);
		// Act
		tracing.trackAndCorrelateServiceBusDependency(
			SB_ENDPOINT, 
			SB_QUEUE_NAME, 
			messageMock, 
			(m) -> {
				try {
					Thread.sleep(DELAY_COMPLETION_MS);
					result.complete(null);
				} catch (InterruptedException e) {
					// shallowed exception is ok in here, please expect this test to timeout
				}
				
				return result;
			});

		while (!existsTrackDepedency(mockingDetails(telemetryClientMock)
										.getInvocations())){
			Thread.sleep(DELAY_COMPLETION_MS / 10);
		}
		
		// Assert
		verify(telemetryClientMock, times(1))
			.trackDependency(any(RemoteDependencyTelemetry.class));
		verify(telemetryClientMock).trackDependency(argument.capture());
		assertTrue(argument.getValue().getDuration().getTotalMilliseconds() >= DELAY_COMPLETION_MS);		
		assertEquals(
			SB_ENDPOINT + " | "+ SB_QUEUE_NAME, 
			argument.getValue().getTarget());
		assertTrue(argument.getValue().getSuccess());
	}

	@Test(expected = CompletionException.class)
	public void trackAndCorrelate_ThenThrownRuntimeExceptionWhenErr() throws Throwable {
		// Arrange
		ThreadContext.setRequestTelemetryContext(telemetryContextMock);
		CompletableFuture<Void> result = new CompletableFuture<>();
		ArgumentCaptor<RemoteDependencyTelemetry> argument = ArgumentCaptor.forClass(RemoteDependencyTelemetry.class);
		
		when(functionMock.apply(eq(messageMock))).thenReturn(result);

		// Act
		tracing.trackAndCorrelateServiceBusDependency(
			SB_ENDPOINT,
			SB_QUEUE_NAME, 
			messageMock,
			functionMock);
	
		try {
			result.completeExceptionally(new RuntimeException());
			result.join();
		}
		finally{
			// Assert
			verify(functionMock, times(1))
				.apply(eq(messageMock));
			verify(telemetryClientMock, times(1))
				.trackException(any(RuntimeException.class));
			verify(telemetryClientMock, times(1))
				.trackDependency(any(RemoteDependencyTelemetry.class));
			verify(telemetryClientMock).trackDependency(argument.capture());
			assertEquals(
				SB_ENDPOINT + " | "+ SB_QUEUE_NAME, 
				argument.getValue().getTarget());
			assertFalse(argument.getValue().getSuccess());
		}
	}

	private static boolean existsTrackDepedency(Collection<Invocation> invcations)
	{
		for (Invocation invoke : invcations) {
			if(invoke.getMethod().getName().equals(TRACK_DEPENDENCY_METHOD_NAME))
			{
				return true;
			}
		}

		return false;
	}
}