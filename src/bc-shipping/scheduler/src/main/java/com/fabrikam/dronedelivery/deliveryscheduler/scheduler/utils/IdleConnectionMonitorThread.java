package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.concurrent.TimeUnit;

import org.apache.http.impl.nio.conn.PoolingNHttpClientConnectionManager;

public class IdleConnectionMonitorThread extends Thread {

	private final PoolingNHttpClientConnectionManager connMgr;
	private volatile boolean shutdown;

	public IdleConnectionMonitorThread(PoolingNHttpClientConnectionManager poolingConnManager) {
		super();
		this.connMgr = poolingConnManager;
	}

	@Override
	public void run() {
		try {
			while (!shutdown) {
				synchronized (this) {
					wait(1000);
					// Close expired connections
					connMgr.closeExpiredConnections();
					// Optionally, close connections
					// that have been idle longer than 30 sec
					connMgr.closeIdleConnections(30, TimeUnit.SECONDS);
				}
			}
		} catch (InterruptedException ex) {
			// terminate
			shutdown();
		}
	}

	public void shutdown() {
		shutdown = true;
		synchronized (this) {
			notifyAll();
		}
	}

}
