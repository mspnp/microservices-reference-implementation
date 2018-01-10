package com.fabrikam.dronedelivery.ingestion.models;

import org.springframework.stereotype.Component;

@Component
public class PackageInfo {

	private String packageId;
	private ContainerSize size;
    private double weight;
    private String tag;


	public String getPackageId() {
		return this.packageId;
	}

	public void setPackageId(String packageId) {
		this.packageId = packageId;
	}

	public ContainerSize getSize() {
		return size;
	}

	public void setSize(ContainerSize size) {
		this.size = size;
	}

	public double getWeight() {
		return weight;
	}

	public void setWeight(double weight) {
		this.weight = weight;
	}

	public String getTag() {
		return tag;
	}

	public void setTag(String tag) {
		this.tag = tag;
	}

	@Override
	public String toString() {
		return "PackageInfo [packageId=" + packageId + ", size=" + size.name() + ", weight=" + weight + ", tag=" + tag + "]";
	}
}
