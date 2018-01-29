package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.utils;

import java.util.ArrayList;
import java.util.List;

import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.ConfirmationType;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageDetail;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker.PackageSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ConfirmationRequired;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.ContainerSize;
import com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.receiver.PackageInfo;

public class ModelsConverter {
	public static PackageDetail getPackageDetail(PackageInfo packageInfo) {
		PackageDetail packageDetail = new PackageDetail();
		packageDetail.setId(packageInfo.getPackageId());
		packageDetail.setSize(getPackageSize(packageInfo.getSize()));
		return packageDetail;
	}

	public static List<PackageDetail> getListOfPackageDetail(List<PackageInfo> packages) {
		List<PackageDetail> listOfPackages = new ArrayList<PackageDetail>();
		for (PackageInfo packageInfo : packages) {
			PackageDetail packageDetail = new PackageDetail();
			packageDetail.setId(packageInfo.getPackageId());
			packageDetail.setSize(getPackageSize(packageInfo.getSize()));
			listOfPackages.add(packageDetail);
		}

		return listOfPackages;
	}

	public static PackageSize getPackageSize(ContainerSize containerSize) {
		return containerSize != null ? PackageSize.values()[containerSize.ordinal()] : PackageSize.Small;
	}

	public static ConfirmationType getConfirmationType(ConfirmationRequired confirm) {
		return confirm != null ? ConfirmationType.values()[confirm.ordinal()] : ConfirmationType.None;
	}
}
