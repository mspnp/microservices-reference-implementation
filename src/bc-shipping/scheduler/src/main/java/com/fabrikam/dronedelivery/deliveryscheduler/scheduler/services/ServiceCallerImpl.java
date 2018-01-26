package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

import java.util.Collections;

import org.apache.commons.lang3.StringUtils;
import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.http.ConnectionReuseStrategy;
import org.apache.http.HttpHost;
import org.apache.http.HttpResponse;
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
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.client.HttpComponentsAsyncClientHttpRequestFactory;
import org.springframework.util.concurrent.ListenableFuture;
import org.springframework.web.client.AsyncRestTemplate;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.IdleConnectionMonitorThread;

public abstract class ServiceCallerImpl implements ServiceCaller {
	private HttpHeaders requestHeaders;
	private final static Logger Log = LogManager.getLogger(ServiceCallerImpl.class);
	private final static int Second = 1000;
	
	public AsyncRestTemplate getAsyncRestTemplate() {
		AsyncRestTemplate asyncRestTemplate = new AsyncRestTemplate(getCustomClientHttpRequestFactory()); //CustomClientHttpRequestFactory.INSTANCE.get()
		asyncRestTemplate.setErrorHandler(new ServiceCallerResponseErrorHandler());
		return asyncRestTemplate;
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
		// Initialize http headers
		initHttpHeaders();
	}

	private void initHttpHeaders() {
		this.requestHeaders = new HttpHeaders();
		this.requestHeaders.setAccept(Collections.singletonList(MediaType.APPLICATION_JSON));
		this.requestHeaders.setContentType(MediaType.APPLICATION_JSON_UTF8);
	}

	public HttpComponentsAsyncClientHttpRequestFactory getCustomClientHttpRequestFactory() {
	    PoolingNHttpClientConnectionManager poolingConnManager = 
	    	      new PoolingNHttpClientConnectionManager(getIOReactor());
	    
		HttpHost httpProxy = getHttpProxy();
		
		CloseableHttpAsyncClient client = getCustomHttpClient(poolingConnManager, httpProxy);

		// Start idle connection monitor
		IdleConnectionMonitorThread  staleMonitor = new IdleConnectionMonitorThread(poolingConnManager);
		staleMonitor.start();

		return getCustomClientHttpRequestFactory(client);
	}
	
	private ConnectingIOReactor getIOReactor() {
		ConnectingIOReactor ioReactor = null;
		
	    try {
			ioReactor = new DefaultConnectingIOReactor();
		} catch (IOReactorException e) {
			Log.error(ExceptionUtils.getStackTrace(e));
		}
		return ioReactor;
	}
	
	private HttpHost getHttpProxy() {
		HttpHost httpProxy = null;
		if (StringUtils.isNotEmpty(SchedulerSettings.HttpProxyValue)) {
			String[] address = SchedulerSettings.HttpProxyValue.split("\\s*:\\s*");
			httpProxy = new HttpHost(address[0], Integer.parseInt(address[1]));
		}
		return httpProxy;
	}

	private HttpComponentsAsyncClientHttpRequestFactory getCustomClientHttpRequestFactory(
			CloseableHttpAsyncClient client) {
		
		HttpComponentsAsyncClientHttpRequestFactory clientHttpRequestFactory = new HttpComponentsAsyncClientHttpRequestFactory();
		clientHttpRequestFactory.setConnectionRequestTimeout(30*Second);
		clientHttpRequestFactory.setConnectTimeout(30*Second);
		clientHttpRequestFactory.setBufferRequestBody(false);
		clientHttpRequestFactory.setReadTimeout(0);

		clientHttpRequestFactory.setHttpAsyncClient(client);
		return clientHttpRequestFactory;
	}
	
	private CloseableHttpAsyncClient getCustomHttpClient(PoolingNHttpClientConnectionManager poolingConnManager,
			HttpHost httpProxy) {
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
		return client;
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