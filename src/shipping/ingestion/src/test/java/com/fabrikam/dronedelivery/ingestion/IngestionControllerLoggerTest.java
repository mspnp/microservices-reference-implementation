package com.fabrikam.dronedelivery.ingestion;

import static org.hamcrest.CoreMatchers.containsString;
import org.apache.logging.log4j.LogManager;
import static org.mockito.Mockito.times;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.asyncDispatch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import com.microsoft.azure.servicebus.ServiceBusException;


import java.util.Date;
import java.util.UUID;

import org.apache.logging.log4j.Logger;
import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.mockito.InjectMocks;
//import org.mockito.runners.MockitoJUnitRunner;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.powermock.api.mockito.PowerMockito;
import org.powermock.core.classloader.annotations.PrepareForTest;
import org.powermock.modules.junit4.legacy.PowerMockRunner;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
//import org.springframework.test.web.servlet.setup.MockMvcBuilders;
//import org.springframework.web.context.WebApplicationContext;
//import org.springframework.web.context.WebApplicationContext;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;

import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;
import com.fabrikam.dronedelivery.ingestion.controller.IngestionController;
import com.fabrikam.dronedelivery.ingestion.models.ConfirmationRequired;
import com.fabrikam.dronedelivery.ingestion.models.ContainerSize;
import com.fabrikam.dronedelivery.ingestion.models.DeliveryBase;
import com.fabrikam.dronedelivery.ingestion.models.ExternalDelivery;
import com.fabrikam.dronedelivery.ingestion.models.ExternalRescheduledDelivery;
import com.fabrikam.dronedelivery.ingestion.models.PackageInfo;
import com.fabrikam.dronedelivery.ingestion.service.IngestionImpl;
import com.fasterxml.jackson.core.JsonParseException;
import com.fasterxml.jackson.databind.ObjectMapper;

import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.header;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.request;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;

import java.io.IOException;

@RunWith(PowerMockRunner.class)
@PrepareForTest({LogManager.class})
public class IngestionControllerLoggerTest {

	private MockMvc mockMvc;
	private PackageInfo packageInfo;
	private ExternalDelivery externalDelivery;
	ExternalRescheduledDelivery externalRDelivery;
	
	@Mock
	private IngestionImpl ingestionimplMock;
	 
	@Mock
	private ApplicationProperties appPropsMock;
	
	@Mock
	private Logger loggerMock;

	@InjectMocks
	private IngestionController ingestionController;

	@Before
	public void setUp() throws Exception {
		
		

			
		MockitoAnnotations.initMocks(this);
	    mockMvc = MockMvcBuilders
	                .standaloneSetup(ingestionController)
	                .build();
		

		packageInfo = new PackageInfo();
		packageInfo.setSize(ContainerSize.Large);
		packageInfo.setPackageId(UUID.randomUUID().toString());

		externalDelivery = new ExternalDelivery();
		externalDelivery.setOwnerId(UUID.randomUUID().toString());
		externalDelivery.setPickupTime(new Date());
		externalDelivery.setDropOffLocation("Austin");
		externalDelivery.setPickupLocation("Texas");
		externalDelivery.setExpedited(false);
		externalDelivery.setConfirmationRequired(ConfirmationRequired.FingerPrint);
		externalDelivery.setDeadline("LineOfDeadlyZombiatedPeople");
		externalDelivery.setPackageInfo(packageInfo);

		externalRDelivery = new ExternalRescheduledDelivery();
		externalRDelivery.setDeadline("deadline");
		externalRDelivery.setDeliveryId(UUID.randomUUID().toString());
		externalRDelivery.setDropOffLocation("location");
		externalRDelivery.setPickupLocation("location");
		
		
	    PowerMockito.mockStatic(LogManager.class);
	    Mockito.when(LogManager.getLogger(IngestionController.class)).thenReturn(loggerMock);
		
		
		
		
		

	}

	@After
	public void tearDown() throws Exception {
	}
    	
	@Test
	public void ScheduleDeliveryAsyncLogs() throws Exception {
		
		Mockito.doNothing().when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		Mockito.when(appPropsMock.getServiceMeshCorrelationHeader()).thenReturn("header");
		
		MvcResult resultActions = mockMvc.perform(
		            post("/api/deliveryrequests")
		                    .contentType(MediaType.APPLICATION_JSON)
		                    .content(asJsonString(externalDelivery)))
		            .andExpect(request().asyncStarted()).andReturn();
		
		mockMvc.perform(asyncDispatch(resultActions))
		.andExpect(header().string("Content-Type", containsString("application/json")))
		.andExpect(status().isAccepted());
				
		 Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		  Mockito.verify(appPropsMock, times(1)).getServiceMeshCorrelationHeader();
		    
	}
	
	
	
	
	
	
	
	
	private static String asJsonString(final Object obj) {
		try {
			final ObjectMapper mapper = new ObjectMapper();
			final String jsonContent = mapper.writeValueAsString(obj);
			return jsonContent;
		} catch (Exception e) {
			throw new RuntimeException(e);
		}
	}

}
