// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

package com.fabrikam.dronedelivery.ingestion;

import com.fabrikam.dronedelivery.ingestion.util.InstrumentedQueueClientImpl;
import com.fabrikam.dronedelivery.ingestion.util.ServiceBusTracing;
import com.microsoft.azure.servicebus.IMessage;
import com.microsoft.azure.servicebus.IQueueClient;

import org.junit.Before;
import org.junit.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;

import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.*;

public class InstrumentedQueueClientTest {
	
	private static final String SB_ENDPOINT = "sbEndpoint";

	private static final String SB_QUEUE_NAME = "sbQueueName";

	private @Mock ServiceBusTracing tracingMock;

	private @Mock IMessage messageMock;

	private @InjectMocks InstrumentedQueueClientImpl instrumentedQueueClientImpl;

	private IQueueClient queueClientlMock;

	@Before
	public void setUp() throws Exception {
		MockitoAnnotations.initMocks(this);
		queueClientlMock = mock(IQueueClient.class);
		when(queueClientlMock.getQueueName()).thenReturn(SB_QUEUE_NAME);

		instrumentedQueueClientImpl = 
			new InstrumentedQueueClientImpl(
				SB_ENDPOINT, 
				queueClientlMock, 
				tracingMock);
	}

	@Test
	public void sendAsync_ThenWrappedByTrackAndCorrelatedWithProperInfo() throws Exception {

		// Arrange

		// Act
		instrumentedQueueClientImpl.sendAsync(messageMock);

		// Assert
		verify(queueClientlMock, times(1)).getQueueName();
		verify(tracingMock, times(1))
			.trackAndCorrelateServiceBusDependency(
					eq(SB_ENDPOINT), 
					eq(SB_QUEUE_NAME),
					eq(messageMock), 
					Mockito.any());
	}
}
