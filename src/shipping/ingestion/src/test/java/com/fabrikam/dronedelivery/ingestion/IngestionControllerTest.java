package com.fabrikam.dronedelivery.ingestion;

import static org.hamcrest.CoreMatchers.containsString;
import static org.mockito.Mockito.times;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.asyncDispatch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;

import java.util.Date;
import java.util.UUID;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.mockito.InjectMocks;
//import org.mockito.runners.MockitoJUnitRunner;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
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



public class IngestionControllerTest {

	private MockMvc mockMvc;
	private PackageInfo packageInfo;
	private ExternalDelivery externalDelivery;
	ExternalRescheduledDelivery externalRDelivery;
	
	@Mock
	private IngestionImpl ingestionimplMock;
	 
	@Mock
	private ApplicationProperties appPropsMock;
	
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
		
		

	}

	@After
	public void tearDown() throws Exception {
	}
    	
	@Test
	public void ScheduleDeliveryIsAccepted() throws Exception {
		
		Mockito.doNothing().when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
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
		    
	}
	
	@Test
	public void RescheduleDeliveryIsOk() throws Exception {
				
		Mockito.doNothing().when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		MvcResult resultActions = mockMvc.perform(patch("/api/deliveryrequests/" + deliveryId)
				.content(asJsonString(externalRDelivery)).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions))
		.andExpect(header().string("Content-Type", containsString("application/json")))
		.andExpect(status().is2xxSuccessful());
		
		 Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
    
	@Test
	public void CancelDeliveryIsOk() throws Exception {
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		Mockito.doNothing().when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		

		MvcResult resultActions = mockMvc
				.perform(delete("/api/deliveryrequests/" + deliveryId).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();
		
		mockMvc.perform(asyncDispatch(resultActions))
		.andExpect(status().is2xxSuccessful());
		
		Mockito.verify(ingestionimplMock, times(1))
		 .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
	}
    
    
	@Test
	public void ProbeDeliveryIsOk() throws Exception {
		MvcResult resultActions = mockMvc.perform(get("/api/probe/").contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions)).andExpect(status().is2xxSuccessful());
	}
	
	
	@Test
	public void scheduleDeliveryHandlesServiceBusException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new ServiceBusException(true,"message")))
		.when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		

		mockMvc.perform(
	            post("/api/deliveryrequests")
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void schedulerDeliveryHandlesIllegalArgumentException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new IllegalArgumentException()))
		.when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		

		mockMvc.perform(
	            post("/api/deliveryrequests")
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void scheduleDeliveryHandlesIOException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new IOException()))
		.when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		

		mockMvc.perform(
	            post("/api/deliveryrequests")
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void scheduleDeliveryHandlesJsonProcessingException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new JsonParseException(null, "error")))
		.when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		mockMvc.perform(
	            post("/api/deliveryrequests")
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	
	@Test
	public void scheduleDeliveryHandlesMethodArgumentTypeMismatchException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new MethodArgumentTypeMismatchException(appPropsMock, null, null, null, null)))
		.when(ingestionimplMock)
		.scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		mockMvc.perform(
	            post("/api/deliveryrequests")
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		 .scheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	
	//------------------
	
	@Test
	public void rescheduleDeliveryHandlesServiceBusException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new ServiceBusException(true,"message")))
		.when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		mockMvc.perform(
	            patch("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void reschedulerDeliveryHandlesIllegalArgumentException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new IllegalArgumentException()))
		.when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();


		mockMvc.perform(
	            patch("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void rescheduleDeliveryHandlesIOException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new IOException()))
		.when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		mockMvc.perform(
	            patch("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	@Test
	public void rescheduleDeliveryHandlesJsonProcessingException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new JsonParseException(null, "error")))
		.when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		
		mockMvc.perform(
	            patch("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	
	@Test
	public void rescheduleDeliveryHandlesMethodArgumentTypeMismatchException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new MethodArgumentTypeMismatchException(appPropsMock, null, null, null, null)))
		.when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		
		mockMvc.perform(
	            patch("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
	}
	
	
	//---------------------
	
	@Test
	public void cancelscheduleDeliveryHandlesServiceBusException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new ServiceBusException(true,"message")))
		.when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		mockMvc.perform(
	            delete("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		  .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
	}
	
	@Test
	public void cancelschedulerDeliveryHandlesIllegalArgumentException() throws Exception {
						
		Mockito.doThrow(new RuntimeException(new IllegalArgumentException()))
		.when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();


		mockMvc.perform(
	            delete("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		  .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
	}
	
	@Test
	public void cancelscheduleDeliveryHandlesIOException() throws Exception {

						
		Mockito.doThrow(new RuntimeException(new IOException()))
		.when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		mockMvc.perform(
	            delete("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		  .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
	}
	
	@Test
	public void cancelscheduleDeliveryHandlesJsonProcessingException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new JsonParseException(null, "error")))
		.when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		
		mockMvc.perform(
	            delete("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().is5xxServerError());

		  Mockito.verify(ingestionimplMock, times(1))
		 .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
	}
	
	
	@Test
	public void cancelscheduleDeliveryHandlesMethodArgumentTypeMismatchException() throws Exception {
										
		Mockito.doThrow(new RuntimeException(new MethodArgumentTypeMismatchException(appPropsMock, null, null, null, null)))
		.when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		
		mockMvc.perform(
	            delete("/api/deliveryrequests/" + deliveryId)
	                    .contentType(MediaType.APPLICATION_JSON)
	                    .content(asJsonString(externalDelivery)))
						.andExpect(status().isBadRequest());

		  Mockito.verify(ingestionimplMock, times(1))
		  .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
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
