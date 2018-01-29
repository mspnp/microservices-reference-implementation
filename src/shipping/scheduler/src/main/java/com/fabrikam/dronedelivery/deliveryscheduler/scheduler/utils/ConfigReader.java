package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.HashMap;
import java.util.Map;
import java.util.Properties;

import org.apache.commons.lang3.exception.ExceptionUtils;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class ConfigReader {

	private static final Logger Log = LogManager.getLogger(ConfigReader.class);

	private static Map<String, String> getConfig(Properties properties) {
		Map<String, String> config = new HashMap<String, String>(10);
		for (Object id : properties.keySet()) {
			String key = (String) id;
			config.put(key, properties.getProperty(key));
		}

		return config;
	}

	public static void printProperties(Map<String, String> map) {
		for (Map.Entry<String, String> entry : map.entrySet()) {
			Log.error(entry.getKey() + ": " + entry.getValue());
		}
	}

	public static Map<String, String> readAllConfigurationValues(String filename) {
		Map<String, String> appConfig = null;
		Properties properties = new Properties();
		try {
			properties.load(ConfigReader.class.getClassLoader().getResourceAsStream(filename));
			appConfig = getConfig(properties);
		} catch (Exception e) {
			Log.error(ExceptionUtils.getStackTrace(e).toString());
		}

		return appConfig;
	}
}
