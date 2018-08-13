package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.services;

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
import org.springframework.http.client.HttpComponentsAsyncClientHttpRequestFactory;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.SchedulerSettings;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils.IdleConnectionMonitorThread;

public enum CustomClientHttpRequestFactory {
	INSTANCE;
	private final static Logger Log = LogManager.getLogger(CustomClientHttpRequestFactory.class);
	private final static int Second = 1000;

	public HttpComponentsAsyncClientHttpRequestFactory get() {
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
}
