package com.fabrikam.dronedelivery.deliveryscheduler.akkareader;

import akka.actor.ActorSystem;
import akka.stream.ActorMaterializer;
import akka.stream.Materializer;

public class ReactiveStreamingApp {
	private static ActorSystem system = ActorSystem.create("Dispatcher");
	protected final static Materializer streamMaterializer = ActorMaterializer.create(system);
}
