package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import org.apache.http.HeaderElement;
import org.apache.http.HeaderElementIterator;
import org.apache.http.HttpResponse;
import org.apache.http.conn.ConnectionKeepAliveStrategy;
import org.apache.http.message.BasicHeaderElementIterator;
import org.apache.http.protocol.HTTP;
import org.apache.http.protocol.HttpContext;

public class KeepAliveStrategy {

	public static ConnectionKeepAliveStrategy getCustomKeepAliveStrategy() {
		return new ConnectionKeepAliveStrategy() {
			@Override
			public long getKeepAliveDuration(HttpResponse response, HttpContext context) {
				// Honor 'keep-alive' header
				HeaderElementIterator it = new BasicHeaderElementIterator(
						response.headerIterator(HTTP.CONN_KEEP_ALIVE));
				while (it.hasNext()) {
					HeaderElement he = it.nextElement();
					String param = he.getName();
					String value = he.getValue();
					if (value != null && param.equalsIgnoreCase("timeout")) {
						try {
							return Long.parseLong(value) * 1000;
						} catch (NumberFormatException ignore) {
						}
					}
				}

				return Long.MAX_VALUE;
			}
		};
	}
}
