package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;
import java.util.Collections;

import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.client.HttpComponentsAsyncClientHttpRequestFactory;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.IdleConnectionMonitorThread;

import org.apache.http.impl.nio.client.CloseableHttpAsyncClient;
import org.apache.http.impl.nio.client.HttpAsyncClientBuilder;
import org.apache.http.impl.nio.client.HttpAsyncClients;
import org.apache.http.impl.nio.conn.PoolingNHttpClientConnectionManager;
import org.apache.http.impl.nio.reactor.DefaultConnectingIOReactor;
import org.apache.http.nio.reactor.ConnectingIOReactor;
import org.apache.http.nio.reactor.IOReactorException;
import org.apache.http.protocol.HttpContext;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.commons.lang3.StringUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.http.ConnectionReuseStrategy;
import org.apache.http.HttpHost;
import org.apache.http.HttpResponse;

public abstract class ServiceCallerImpl implements ServiceCaller {
	private AsyncRestTemplate asyncRestTemplate;
	private HttpHeaders requestHeaders;
	private static final Logger Log = LogManager.getLogger(ServiceCallerImpl.class);
	private final static int FiveSeconds = 5000;
	
	public AsyncRestTemplate getAsyncRestTemplate() {
		return asyncRestTemplate;
	}

	public void setAsyncRestTemplate(AsyncRestTemplate asyncRest) {
		this.asyncRestTemplate = asyncRest;
	}

	public HttpHeaders getRequestHeaders() {
		return requestHeaders;
	}

	public void setRequestHeaders(HttpHeaders requestHeaders) {
		this.requestHeaders = requestHeaders;
	}

	@Override
	public <T> ListenableFuture<?> getData(String url, T data) {
		return null;
	}

	public ServiceCallerImpl() {
	    ConnectingIOReactor ioReactor = null;
		
	    try {
			ioReactor = new DefaultConnectingIOReactor();
		} catch (IOReactorException e) {
			Log.error(ExceptionUtils.getStackTrace(e));
		}
	    
	    PoolingNHttpClientConnectionManager poolingConnManager = 
	    	      new PoolingNHttpClientConnectionManager(ioReactor);
	    
		HttpHost httpProxy = null;
		if (StringUtils.isNotEmpty(SchedulerSettings.HttpProxyValue)) {
			String[] address = SchedulerSettings.HttpProxyValue.split("\\s*:\\s*");
			httpProxy = new HttpHost(address[0], Integer.parseInt(address[1]));
		}
		
		HttpAsyncClientBuilder builder = HttpAsyncClients.custom().setConnectionManager(poolingConnManager)
				.setConnectionReuseStrategy(new ConnectionReuseStrategy() {

					@Override
					public boolean keepAlive(HttpResponse response, HttpContext context) {
						return true;
					}
				})
				// .setKeepAliveStrategy(KeepAliveStrategy.getCustomKeepAliveStrategy())
				.setMaxConnPerRoute(Integer.MAX_VALUE).setMaxConnTotal(Integer.MAX_VALUE);

		if (httpProxy != null) {
			builder.setProxy(httpProxy);
		}

		CloseableHttpAsyncClient client = builder.build();

		IdleConnectionMonitorThread  staleMonitor = new IdleConnectionMonitorThread(poolingConnManager);
		staleMonitor.start();
		HttpComponentsAsyncClientHttpRequestFactory clientHttpRequestFactory = new HttpComponentsAsyncClientHttpRequestFactory();
		clientHttpRequestFactory.setConnectionRequestTimeout(FiveSeconds);
		clientHttpRequestFactory.setConnectTimeout(FiveSeconds);
		clientHttpRequestFactory.setBufferRequestBody(false);
		clientHttpRequestFactory.setReadTimeout(FiveSeconds);

		clientHttpRequestFactory.setHttpAsyncClient(client);
		asyncRestTemplate = new AsyncRestTemplate(clientHttpRequestFactory);
		this.requestHeaders = new HttpHeaders();
		this.requestHeaders.setAccept(Collections.singletonList(MediaType.APPLICATION_JSON));
		this.requestHeaders.setContentType(MediaType.APPLICATION_JSON_UTF8);
		asyncRestTemplate.setErrorHandler(new ServiceCallerResponseErrorHandler());
	}

	@Override
	public <T> ListenableFuture<?> postData(String url, T entity) {
		return null;
	}
	
	@Override
	public <T> ListenableFuture<?> putData(String url, T entity, Object... args) {
		return null;
	}
}