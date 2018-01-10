package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;

public class PackageGen {

	public PackageGen() {
		// TODO Auto-generated constructor stub
	}
	
	private String id;
	private ContainerSize size;
	private String tag;
	private double weight;
	
	public String getId() {
		return id;
	}
	public void setId(String id) {
		this.id = id;
	}
	public ContainerSize getSize() {
		return size;
	}
	public void setSize(ContainerSize size) {
		this.size = size;
	}
	public String getTag() {
		return tag;
	}
	public void setTag(String tag) {
		this.tag = tag;
	}
	public double getWeight() {
		return weight;
	}
	public void setWeight(double weight) {
		this.weight = weight;
	}
	
	@Override
	public String toString() {
		return "PackageGen [id=" + id + ", size=" + size.name() + ", tag=" + tag + ", weight=" + weight + "]";
	}
}
