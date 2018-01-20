package com.fabrikam.dronedelivery.ingestion;

import static org.hamcrest.CoreMatchers.any;
import static org.hamcrest.CoreMatchers.containsString;
import static org.mockito.Matchers.anyMap;
import static org.mockito.Matchers.eq;
import static org.mockito.Matchers.refEq;
import static org.mockito.Mockito.times;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.asyncDispatch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;

import java.util.Date;
import java.util.Map;
import java.util.UUID;

import org.junit.After;
import org.junit.Before;
import org.junit.Ignore;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.mockito.InjectMocks;
//import org.mockito.runners.MockitoJUnitRunner;
import org.mockito.Mock;
import org.mockito.Mockito;
import org.mockito.MockitoAnnotations;
import org.springframework.beans.factory.annotation.Autowired;

import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;
import org.springframework.web.context.WebApplicationContext;
//import org.springframework.test.web.servlet.setup.MockMvcBuilders;
//import org.springframework.web.context.WebApplicationContext;
//import org.springframework.web.context.WebApplicationContext;

import com.fabrikam.dronedelivery.ingestion.configuration.ApplicationProperties;
import com.fabrikam.dronedelivery.ingestion.controller.IngestionController;
import com.fabrikam.dronedelivery.ingestion.models.ConfirmationRequired;
import com.fabrikam.dronedelivery.ingestion.models.ContainerSize;
import com.fabrikam.dronedelivery.ingestion.models.Delivery;
import com.fabrikam.dronedelivery.ingestion.models.DeliveryBase;
import com.fabrikam.dronedelivery.ingestion.models.ExternalDelivery;
import com.fabrikam.dronedelivery.ingestion.models.ExternalRescheduledDelivery;
import com.fabrikam.dronedelivery.ingestion.models.PackageInfo;
import com.fabrikam.dronedelivery.ingestion.service.Ingestion;
import com.fabrikam.dronedelivery.ingestion.service.IngestionImpl;
import com.fasterxml.jackson.databind.ObjectMapper;

import static org.springframework.test.web.servlet.setup.MockMvcBuilders.webAppContextSetup;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.header;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.request;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import org.springframework.test.context.junit4.SpringRunner;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import java.util.*;

@RunWith(SpringRunner.class)
@SpringBootTest
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
	
	@Test
	public void RescheduleDeliveryIsOk() throws Exception {
				
		Mockito.doNothing().when(ingestionimplMock)
		.rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		
		Mockito.when(appPropsMock.getServiceMeshCorrelationHeader()).thenReturn("header");
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		MvcResult resultActions = mockMvc.perform(patch("/api/deliveryrequests/" + deliveryId)
				.content(asJsonString(externalRDelivery)).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions))
		.andExpect(header().string("Content-Type", containsString("application/json")))
		.andExpect(status().is2xxSuccessful());
		
		 Mockito.verify(ingestionimplMock, times(1))
		 .rescheduleDeliveryAsync(Mockito.any(DeliveryBase.class), Mockito.anyMap());
		  Mockito.verify(appPropsMock, times(1)).getServiceMeshCorrelationHeader();
	}
    
	@Test
	public void CancelDeliveryIsOk() throws Exception {
		String deliveryId = externalRDelivery.getDeliveryId().toString();
		Mockito.doNothing().when(ingestionimplMock)
		.cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		
		Mockito.when(appPropsMock.getServiceMeshCorrelationHeader()).thenReturn("header");

		MvcResult resultActions = mockMvc
				.perform(delete("/api/deliveryrequests/" + deliveryId).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();
		
		mockMvc.perform(asyncDispatch(resultActions))
		.andExpect(status().is2xxSuccessful());
		
		Mockito.verify(ingestionimplMock, times(1))
		 .cancelDeliveryAsync(Mockito.anyString(), Mockito.anyMap());
		  Mockito.verify(appPropsMock, times(1)).getServiceMeshCorrelationHeader();
	}
    
    
	@Test
	public void ProbeDeliveryIsOk() throws Exception {
		MvcResult resultActions = mockMvc.perform(get("/api/probe/").contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions)).andExpect(status().is2xxSuccessful());
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
