import React, { useState } from 'react';
import DroneDeliveryService from '../services/DroneDeliveryService';
import './Request.css';

export const Request = () => {
  const packageSizes = ['Small', 'Medium', 'Large'];
  const [trackingKey, setTrackingKey] = useState('');
  const [packageSize, setPackageSize] = useState('Small');
  const [packageWeight, setPackageWeight] = useState('');
  const [showWaring, setShowWarning] = useState(false);
  const [warning, setWarning] = useState('');

  const onSend = async (event) => {
    event.preventDefault();
    let deliveryRequest = {
      confirmationRequired: "None",
      deadline: "",
      dropOffLocation: "drop off",
      expedited: true,
      ownerId: "myowner",
      packageInfo: {
        packageId: "mypackage",
        size: packageSize,
        tag: "mytag",
        weight: packageWeight
      },
      pickupLocation: "my pickup",
      pickupTime: "2019-05-08T20:00:00.000Z"
    }
    sendDeliveryRequest(deliveryRequest);
  }

  const sendDeliveryRequest = async (deliveryRequest) => {
    const droneDeliveryService = new DroneDeliveryService();
    let deliveryResponse;
    try {
      deliveryResponse = await droneDeliveryService.deliveryRequest(deliveryRequest);
      setTrackingKey(deliveryResponse.deliveryId);
    } catch (error) {
      setShowWarning(true);
      setWarning("Request cannot be processed !!");
    }
  }

  const onPackageWeightChange = (event) => {
    let weight = parseInt(event.target.value);
    setPackageWeight(weight);
  }
  const onPackageSizeChange = (event) => {
    setPackageSize(event.target.value);
  }
  return (
    <div>
      <h1 style={{marginLeft: 8}}>Request delivery</h1>
      <form onSubmit={onSend}>
        <div className="outerContainer">
          <div className="container" >
            <p>Select Package size:</p>
            <select className="custom-input"
              placeholder="Select Package height"
              onChange={onPackageSizeChange}
            >
              {packageSizes.map((option, index) => (
                <option key={index} value={option}>
                  {option}
                </option>
              ))}
            </select>
          </div>
          <div className="container">
            <p>Enter Package weight:</p>
            <input
              className="custom-input"
              placeholder="Enter package weight"
              onChange={onPackageWeightChange}
              type="text"
            />
          </div>
          <input className="main-button" type='submit' value="Request" />
        </div>
      </form>
      <div style={{ padding: 10 }}>
        <p>Delivery Id:</p>
        <textarea style={{ width: '800px', height: '110px', border: '2px solid #008CBA', }} value={trackingKey} type="text"></textarea>
      </div>
      {showWaring && <span style={{ paddingLeft: 10, color: 'red' }}>{warning}</span>}
    </div>
  );
}
