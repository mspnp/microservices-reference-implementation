package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import static net.javacrumbs.futureconverter.springjava.FutureConverter.toCompletableFuture;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

import org.apache.commons.lang3.exception.ExceptionUtils;

import org.springframework.http.HttpEntity;
import org.springframework.http.HttpMethod;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.util.concurrent.ListenableFuture;


import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class PackageServiceCallerImpl extends ServiceCallerImpl {

	private final static JsonParser jsonParser = new JsonParser();
	
	private PackageGen packageGen = null;

	// Calls the super constructor and sets the HTTP context
	public PackageServiceCallerImpl() {
		super();
	}

	@Override
	public <T> ListenableFuture<?> getData(String url, T data) {
		// TODO implement get methods of package service
		return null;
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
		ListenableFuture<ResponseEntity<String>> response = getAsyncRestTemplate().postForEntity(url, httpEntity,
				String.class, entity);

		return response;
	}
	
	@Override
	public <T> ListenableFuture<?> putData(String url, T entity, Object... args) {
		HttpEntity<T> httpEntity = new HttpEntity<T>(entity, this.getRequestHeaders());
		ListenableFuture<ResponseEntity<String>> response = getAsyncRestTemplate().exchange(url, HttpMethod.PUT, httpEntity, String.class);

		return response;
	}

	@SuppressWarnings("unchecked")
	public PackageGen createPackage(PackageInfo packageInfo, String uri)
			throws InterruptedException, ExecutionException {
		JsonObject packObj = new JsonObject();

		packObj.addProperty("size", packageInfo.getSize().name());
		packObj.addProperty("tag", packageInfo.getTag());
		packObj.addProperty("weight", packageInfo.getWeight());

		ListenableFuture<?> response = this.putData(uri+'/' + packageInfo.getPackageId(), packObj.toString());
		ResponseEntity<String> entity = (ResponseEntity<String>) response.get();

		return (entity.getStatusCode() == HttpStatus.CREATED ? getPackageGen(entity.getBody()) : null);
	}
	
	@SuppressWarnings("unchecked")
	public PackageGen createPackageAsync(PackageInfo packageInfo, String uri) {
		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.putData(uri + '/' + packageInfo.getTag(), packageInfo);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			if (response.getStatusCode() == HttpStatus.CREATED) {
				packageGen = getPackageGen(response.getBody());
			} else {
				throw new BackendServiceCallFailedException(response.getStatusCode().getReasonPhrase());
			}
		}).exceptionally(e -> {
			throw new BackendServiceCallFailedException(ExceptionUtils.getStackTrace(e));
		});
		
		future = null;
		cfuture = null;
		packageInfo = null;

		return packageGen;
	}

	private PackageGen getPackageGen(String jsonStr) {
		PackageGen pack = new PackageGen();
		JsonElement jsonElem = jsonParser.parse(jsonStr);
		JsonObject jObject = jsonElem.getAsJsonObject();
		pack.setId(jObject.get("id").getAsString());
		pack.setSize(ContainerSize.valueOf(jObject.get("size").getAsString()));
		pack.setTag(jObject.get("tag").getAsString());

		JsonElement weight = jObject.get("weight");
		pack.setWeight(weight.isJsonNull() ? 0.0 : weight.getAsDouble());

		return pack;
	}
}
