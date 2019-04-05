package com.fabrikam.dronedelivery.ingestion.controller;

import org.springframework.web.bind.annotation.RestController;

import com.fabrikam.dronedelivery.ingestion.models.*;
import com.fabrikam.dronedelivery.ingestion.service.*;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;

import javax.servlet.http.HttpServletResponse;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.slf4j.MDC;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import com.microsoft.azure.servicebus.primitives.ServiceBusException;
import java.io.IOException;
import com.fasterxml.jackson.core.JsonProcessingException;

@RestController
public class IngestionController {

	private final static Logger log = LoggerFactory.getLogger(IngestionController.class);
	
	@Autowired
	private Ingestion ingestion;

	@Autowired
	public IngestionController(Ingestion ingestion) {
		this.ingestion = ingestion;
	}

	@ResponseStatus(value = HttpStatus.BAD_REQUEST, reason = "Bad Format data") // 400
	@ExceptionHandler(IllegalArgumentException.class)
	public void exHandlerBadArgument(IllegalArgumentException e) {
		log.error("Bad data format - ", e);
	}

	@ResponseStatus(value = HttpStatus.BAD_REQUEST, reason = "Method argument exception") // 400
	@ExceptionHandler(MethodArgumentTypeMismatchException.class)
	public void exHandlerBadFormat(MethodArgumentTypeMismatchException e) {
		log.error("Method argument exception - ", e);
	}

	@ResponseStatus(value = HttpStatus.INTERNAL_SERVER_ERROR, reason = "Internal Server Error") // 500
	@ExceptionHandler(ServiceBusException.class)
	public void exHandlerServiceBusError(ServiceBusException e) {
		log.error("ServiceBus exception - ", e);
	}

	@ResponseStatus(value = HttpStatus.GATEWAY_TIMEOUT, reason = "Bad Format data sent to service") // 504
	@ExceptionHandler(IOException.class)
	public void exHandlerIoError(IOException e) {
		log.error("IO exception - ", e);
	}

	@ResponseStatus(value = HttpStatus.INTERNAL_SERVER_ERROR, reason = "Json format exception") // 504
	@ExceptionHandler(JsonProcessingException.class)
	public void exHandlerJsonError(JsonProcessingException e) {
		log.error("Json format exception - ", e);
	}

	@RequestMapping(value = "/api/deliveryrequests", method = RequestMethod.POST, produces = MediaType.APPLICATION_JSON_VALUE, consumes = MediaType.APPLICATION_JSON_VALUE)
	@ResponseBody
	public CompletableFuture<ResponseEntity<ExternalDelivery>> scheduleDeliveryAsync(HttpServletResponse response,
			@RequestBody ExternalDelivery externalDelivery, @RequestHeader HttpHeaders httpHeaders) {
		
		String deliveryId = UUID.randomUUID().toString();

		try {
			MDC.put("DeliveryId", deliveryId);

			log.info("In schedule delivery action with delivery request {}", externalDelivery.toString());

			// Exceptions handled by exception handler
			// making standard in the controller
			DeliveryBase delivery = new Delivery(deliveryId, externalDelivery.getOwnerId().toString(),
					externalDelivery.getPickupLocation(), externalDelivery.getDropOffLocation(),
					externalDelivery.getPickupTime(), externalDelivery.isExpedited(),
					externalDelivery.getConfirmationRequired(), externalDelivery.getPackageInfo(),
					externalDelivery.getDeadline());

			externalDelivery.setDeliveryId(delivery.getDeliveryId());
			response.setHeader("Content-Type", "application/json;charset=utf-8");
			response.setHeader("Location", "http://deliveries/api/deliveries/" + delivery.getDeliveryId());

			// Extract the headers as a map and pass on to eventhub message
			// dumper
			ingestion.scheduleDeliveryAsync(delivery, httpHeaders.toSingleValueMap());
        } finally {
            MDC.remove("DeliveryId");
		}

		return CompletableFuture.completedFuture(new ResponseEntity<>(externalDelivery, HttpStatus.ACCEPTED));

	}

	@RequestMapping(value = "/api/deliveryrequests/{id}", method = RequestMethod.DELETE)
	@ResponseBody
	public CompletableFuture<ResponseEntity<String>> cancelDeliveryAsync(HttpServletResponse response,
			@PathVariable("id") String deliveryId, @RequestHeader HttpHeaders httpHeaders) {

		// Exceptions handled by exception handler
		// making standard in the controller
		try {
			MDC.put("DeliveryId", deliveryId);

			log.info("In cancel delivery action with id: {}", deliveryId);
			ingestion.cancelDeliveryAsync(deliveryId.toString(), httpHeaders.toSingleValueMap());
		} finally {
            MDC.remove("DeliveryId");
		}

		return CompletableFuture.completedFuture(new ResponseEntity<>(deliveryId, HttpStatus.NO_CONTENT));
	}

	@RequestMapping(value = "/api/deliveryrequests/{id}", method = RequestMethod.PATCH, produces = MediaType.APPLICATION_JSON_VALUE, consumes = MediaType.APPLICATION_JSON_VALUE)
	@ResponseBody
	public CompletableFuture<ResponseEntity<String>> rescheduleDeliveryAsync(HttpServletResponse response,
			@RequestBody ExternalRescheduledDelivery externalRescheduledDelivery, @PathVariable("id") String deliveryId, @RequestHeader HttpHeaders httpHeaders) {
		try {
			MDC.put("DeliveryId", deliveryId);

			log.info("In reschedule delivery action with delivery request: {}", externalRescheduledDelivery.toString());

			// Exceptions handled by exception handler
			// making standard in the controller
			// pickup dropoff deadline deliveryid
			response.setHeader("Content-Type", "application/json; charset=utf-8");

			RescheduledDelivery rescheduledDelivery = new RescheduledDelivery(deliveryId,
					externalRescheduledDelivery.getPickupLocation(), externalRescheduledDelivery.getDropOffLocation(),
					externalRescheduledDelivery.getDeadline(), externalRescheduledDelivery.getPickupTime());

			ingestion.rescheduleDeliveryAsync(rescheduledDelivery, httpHeaders.toSingleValueMap());
        } finally {
            MDC.remove("DeliveryId");
		}

		return CompletableFuture.completedFuture(new ResponseEntity<>(deliveryId, HttpStatus.OK));
	}

	@RequestMapping(value = "/api/probe", method = RequestMethod.GET, produces = MediaType.APPLICATION_JSON_VALUE)
	@ResponseBody
	public CompletableFuture<ResponseEntity<HttpStatus>> probeDeliveryAsync() {
		//this endpoint is used for readiness probe
		//implement logic to test readiness
		return CompletableFuture.completedFuture(new ResponseEntity<>(HttpStatus.OK));
	}
}