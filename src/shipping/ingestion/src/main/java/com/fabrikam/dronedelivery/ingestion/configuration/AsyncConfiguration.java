package com.fabrikam.dronedelivery.ingestion.configuration;

import org.springframework.aop.interceptor.AsyncUncaughtExceptionHandler;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.scheduling.annotation.AsyncConfigurer;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.scheduling.concurrent.ThreadPoolTaskExecutor;

import java.util.concurrent.Executor;

@Configuration
@EnableScheduling
public class AsyncConfiguration implements AsyncConfigurer {

	private final ApplicationProperties applicationProperties;
	private final String ThreadNamePrefix = "async-Executor-";

	@Autowired
	public AsyncConfiguration(ApplicationProperties applicationProperties) {
		this.applicationProperties = applicationProperties;
	}

	@Override
	@Bean
	public Executor getAsyncExecutor() {

		ThreadPoolTaskExecutor executor = new ThreadPoolTaskExecutor();
		executor.setCorePoolSize(this.applicationProperties.getThreadPoolExecutorPoolSize());
		executor.setMaxPoolSize(this.applicationProperties.getThreadPoolExecutorMaxPoolSize());
		executor.setQueueCapacity(this.applicationProperties.getThreadPoolExecutorQueueSize());
		executor.setThreadNamePrefix(ThreadNamePrefix);
		executor.initialize();
		return executor;
	}

	@Override
	public AsyncUncaughtExceptionHandler getAsyncUncaughtExceptionHandler() {
		return new AsyncExceptionHandler();
	}

}
