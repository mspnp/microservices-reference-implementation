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
import org.springframework.util.concurrent.ListenableFutureCallback;
//import org.springframework.util.concurrent.ListenableFutureCallback;
import org.springframework.web.context.request.async.DeferredResult;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageGen;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

public class PackageServiceCallerImpl extends ServiceCallerImpl {

	private final static JsonParser jsonParser = new JsonParser();

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

		ListenableFuture<?> response = this.postData(uri, packObj.toString());
		ResponseEntity<String> entity = (ResponseEntity<String>) response.get();

		return (entity.getStatusCode() == HttpStatus.CREATED ? getPackageGen(entity.getBody()) : null);
	}

	@SuppressWarnings("unchecked")
	public DeferredResult<PackageGen> createPackageAsync(PackageInfo packageInfo, String uri) {
		// Create DeferredResult with timeout 5s
		final DeferredResult<PackageGen> result = new DeferredResult<PackageGen>((long)5000);

		// Let's call the backend
		ListenableFuture<ResponseEntity<String>> future = (ListenableFuture<ResponseEntity<String>>) this
				.postData(uri, packageInfo);
		
		CompletableFuture<ResponseEntity<String>> cfuture = toCompletableFuture(future);

		cfuture.thenAcceptAsync(response -> {
			if (response.getStatusCode() == HttpStatus.CREATED) {
				result.setResult(getPackageGen(response.getBody()));
			} else {
				result.setResult(null);
				result.setErrorResult(response.getStatusCode());
			}
		}).exceptionally(e -> {
			result.setErrorResult(ExceptionUtils.getStackTrace(e));
			result.setResult(null);
			return null;
		});

//		future.addCallback(new ListenableFutureCallback<ResponseEntity<String>>() {
//			@Override
//			public void onSuccess(ResponseEntity<String> response) {
//				// Will be called in HttpClient thread
//				if (response.getStatusCode() == HttpStatus.CREATED) {
//					result.setResult(getPackageGen(response.getBody()));
//				}else {
//					result.setResult(null);
//					result.setErrorResult(response.getStatusCode());
//				}
//			}
//
//			@Override
//			public void onFailure(Throwable t) {
//				result.setErrorResult(ExceptionUtils.getStackTrace(t));
//				result.setResult(null);
//			}
//		});

		// Return the thread to servlet container, the response will be
		// processed by another thread.
		return result;
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
