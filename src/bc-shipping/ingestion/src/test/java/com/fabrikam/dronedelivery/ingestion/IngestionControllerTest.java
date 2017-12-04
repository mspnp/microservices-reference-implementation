package com.fabrikam.dronedelivery.ingestion;

import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.asyncDispatch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;

import java.util.Date;
import java.util.UUID;

import org.junit.After;
import org.junit.Before;
import org.junit.Test;
import org.junit.runner.RunWith;
//import org.mockito.runners.MockitoJUnitRunner;

import org.springframework.beans.factory.annotation.Autowired;

import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.MvcResult;
import org.springframework.web.context.WebApplicationContext;
//import org.springframework.test.web.servlet.setup.MockMvcBuilders;
//import org.springframework.web.context.WebApplicationContext;
//import org.springframework.web.context.WebApplicationContext;

import com.fabrikam.dronedelivery.ingestion.models.ConfirmationRequired;
import com.fabrikam.dronedelivery.ingestion.models.ContainerSize;
import com.fabrikam.dronedelivery.ingestion.models.ExternalDelivery;
import com.fabrikam.dronedelivery.ingestion.models.ExternalRescheduledDelivery;
import com.fabrikam.dronedelivery.ingestion.models.PackageInfo;
import com.fasterxml.jackson.databind.ObjectMapper;

import static org.springframework.test.web.servlet.setup.MockMvcBuilders.webAppContextSetup;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.request;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;
import org.springframework.test.context.junit4.SpringRunner;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;


@RunWith(SpringRunner.class)
@SpringBootTest
public class IngestionControllerTest {

	@Autowired
	private WebApplicationContext wac;
	private MockMvc mockMvc;
	private PackageInfo packageInfo;
	private ExternalDelivery delivery;
	ExternalRescheduledDelivery externalRDelivery;

	@Before
	public void setUp() throws Exception {
		mockMvc = webAppContextSetup(this.wac).build();
		packageInfo = new PackageInfo();
		packageInfo.setSize(ContainerSize.Large);
		packageInfo.setPackageId(UUID.randomUUID().toString());

		delivery = new ExternalDelivery();
		delivery.setOwnerId(UUID.randomUUID().toString());
		delivery.setPickupTime(new Date());
		delivery.setDropOffLocation("Austin");
		delivery.setPickupLocation("Texas");
		delivery.setExpedited(false);
		delivery.setConfirmationRequired(ConfirmationRequired.FingerPrint);
		delivery.setDeadline("LineOfDeadlyZombiatedPeople");
		delivery.setPackageInfo(packageInfo);

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
	public void ScheduleDeliveryIsOk() throws Exception {
		MvcResult resultActions = mockMvc.perform(
				post("/api/deliveryrequests").content(asJsonString(delivery)).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions)).andExpect(status().is2xxSuccessful());
	}

	@Test
	public void RescheduleDeliveryIsOk() throws Exception {
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		MvcResult resultActions = mockMvc.perform(patch("/api/deliveryrequests/" + deliveryId)
				.content(asJsonString(externalRDelivery)).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions)).andExpect(status().is2xxSuccessful());
	}

	@Test
	public void CancelDeliveryIsOk() throws Exception {
		String deliveryId = externalRDelivery.getDeliveryId().toString();

		MvcResult resultActions = mockMvc
				.perform(delete("/api/deliveryrequests/" + deliveryId).contentType(MediaType.APPLICATION_JSON))
				.andExpect(request().asyncStarted()).andReturn();

		mockMvc.perform(asyncDispatch(resultActions)).andExpect(status().is2xxSuccessful());
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
