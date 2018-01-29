package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.compensation;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.ServiceName;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.Delivery;

public class RetryableDelivery {
	private Delivery deliveryRequest;
	private ServiceName serviceName;
	private String errorMessage;

	/**
	 * @param deliveryRequest
	 * @param serviceName
	 * @param errorMessage
	 */
	public RetryableDelivery(Delivery deliveryRequest, ServiceName serviceName, String errorMessage) {
		this.deliveryRequest = deliveryRequest;
		this.serviceName = serviceName;
		this.errorMessage = errorMessage;
	}

	public Delivery getDeliveryRequest() {
		return deliveryRequest;
	}

	public void setDeliveryRequest(Delivery deliveryRequest) {
		this.deliveryRequest = deliveryRequest;
	}

	public ServiceName getServiceName() {
		return serviceName;
	}

	public void setServiceName(ServiceName serviceName) {
		this.serviceName = serviceName;
	}

	public String getErrorMessage() {
		return errorMessage;
	}

	public void setErrorMessage(String errorMessage) {
		this.errorMessage = errorMessage;
	}
}
