package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

public class PackageDetail {
	private String Id;
	private PackageSize Size;
	
	public PackageDetail() {

	}

	public String getId() {
		return Id;
	}

	public void setId(String id) {
		Id = id;
	}

	public PackageSize getSize() {
		return Size;
	}

	public void setSize(PackageSize size) {
		Size = size;
	}
	
	public PackageDetail(String id, PackageSize size){
		this.Id = id;
		this.Size = size;
	}
}
