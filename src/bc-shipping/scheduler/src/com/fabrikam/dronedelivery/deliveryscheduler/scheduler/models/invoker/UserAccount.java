package com.fabrikam.dronedelivery.deliveryscheduler.scheduler.models.invoker;

public class UserAccount {
	private String userId;
	private String accountId;

	public UserAccount(){
		
	}
	
	public UserAccount(String userId, String accountId) {
		this.userId = userId;
		this.accountId = accountId;
	}

	public String getUserId() {
		return userId;
	}

	public void setUserId(String userId) {
		this.userId = userId;
	}

	public String getAccountId() {
		return accountId;
	}

	public void setAccountId(String accountId) {
		this.accountId = accountId;
	}

	@Override
	public String toString() {
		return "UserAccount [userId=" + userId + ", accountId=" + accountId + "]";
	}

}
