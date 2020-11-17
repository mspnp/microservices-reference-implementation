import React, { useState } from 'react';
import DroneDeliveryService from '../services/DroneDeliveryService';
import './Request.css';
import { css } from "@emotion/core";
import ScaleLoader from "react-spinners/ScaleLoader"

export const Request = () => {
  const packageSizes = ['Small', 'Medium', 'Large'];
  const [trackingKey, setTrackingKey] = useState('');
  const [packageSize, setPackageSize] = useState('Small');
  const [packageWeight, setPackageWeight] = useState('');
  const [showValidation, setShowValidation] = useState(false);
  const [validation, setValidation] = useState(false);
  const [showErrorMessage, setShowErrorMessage] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [loading, setLoading] = useState(false);

  const onSend = async (event) => {
    event.preventDefault();
    setTrackingKey('');
    if (packageWeight) {
      setShowValidation(false);
      let deliveryRequest = {
        confirmationRequired: "None",
        deadline: "",
        dropOffLocation: "555 110th Ave NE, Bellevue, WA 98004",
        expedited: true,
        ownerId: "myowner",
        packageInfo: {
          packageId: "mypackage",
          size: packageSize,
          tag: "mytag",
          weight: packageWeight
        },
        pickupLocation: "1 Microsoft Way, Redmond, WA 98052",
        pickupTime: "2019-05-08T20:00:00.000Z"
      }
      sendDeliveryRequest(deliveryRequest);
    } else {
      setShowValidation(true);
      setValidation("Input package weight !!");
    }
  }

  const sendDeliveryRequest = async (deliveryRequest) => {
    setLoading(true);
    const droneDeliveryService = new DroneDeliveryService();
    let deliveryResponse;
      try {
          deliveryResponse = await droneDeliveryService.deliveryRequest(deliveryRequest);
          setTrackingKey(deliveryResponse.deliveryId);
      } catch (error) {
          setShowErrorMessage(true);
          setErrorMessage(error.message);
      } finally {
          setLoading(false);
      }
  }

  const onPackageWeightChange = (event) => {
    let weight = parseInt(event.target.value);
    if (!weight) {
      setShowValidation(true);
      setValidation("Input package weight !!");
    } else {
      setShowValidation(false);
    }
    setPackageWeight(weight);
  }

  const onPackageSizeChange = (event) => {
    setPackageSize(event.target.value);
    }

  const override = css`
      display: block;
      margin: 0 auto;
      border-color: red;
      position:relative;
      top: -180px;
      left: 380px;
  `;

  return (
    <div>
      <h2 style={{ marginLeft: 20 }}>Request delivery</h2>
      <form onSubmit={onSend}>
        {showValidation && <span style={{ color: 'red', float: 'right' }}>{validation}</span>}
        <div style={{ marginBottom: 40 }}>
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
              className={showValidation ? 'custom-input error' : 'custom-input'}
              placeholder="Enter package weight"
              onChange={onPackageWeightChange}
              type="number"
            />
          </div>

          <div className="request-button-container">
                <input className="main-button" type='submit' value="Request" />
          </div>
        </div>
        <div style={{ marginLeft: 20 }}>
          <p>Tracking ID:</p>
          <textarea style={{ width: '800px', height: '110px', border: '2px solid #008CBA', }} value={trackingKey} type="text"></textarea>
        </div>
      </form>

        {showErrorMessage && <span style={{ marginLeft: 19, color: 'red' }}>{errorMessage}</span>}

        <div className="sweet-loading">
            <ScaleLoader
                css={override}
                size={50}
                  color={"#484848"}
                loading={loading}
            />
        </div>
    </div>

  );
}
