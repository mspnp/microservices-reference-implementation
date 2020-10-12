import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import DroneDeliveryService from '../services/DroneDeliveryService';

export const DroneDeliveryTracker = () => {
    const [droneLocationPoints, setDroneLocations] = useState([]);
    const [trackingId, setTrackingId] = useState();
    const [currentLocation, setCurrentLocation] = useState([]);
    const [showWarning, setShowWarning] = useState(false);
    const [warning, setWarning] = useState('');

    const onTrack = async () => {
        const droneDeliveryService = new DroneDeliveryService();
        let delivery;
        let droneLocation;
        try {
            delivery = await droneDeliveryService.getDelivery(trackingId);
            droneLocation = await droneDeliveryService.getDroneLocation(trackingId);
        } catch (error) {
            setShowWarning(true);
            setWarning("Request cannot be processed!!");
        }
        if (delivery && delivery.id) {
            const locationPoints = populateLocations(delivery, droneLocation);
            setDroneLocations(locationPoints);
            setShowWarning(false);
        } else {
            setShowWarning(true);
            setWarning('No data available');
            setDroneLocations([])
        }
    }


    const handleInput = (event) => {
        setTrackingId(event.target.value)
    }

    const populateLocations = (delivery, droneLocation) => {

        let locationPoints = [
            {
                location: [delivery.pickup.latitude, delivery.pickup.longitude],
                option: { color: 'blue', title: 'Pick up' },
            },
            {
                location: [droneLocation.lastKnownLocation.latitude, droneLocation.lastKnownLocation.longitude],
                option: { title: 'Drone', icon: 'https://squalldronestorage.blob.core.windows.net/images/mapdrone.png' }
            },
            {
                location: [delivery.dropoff.latitude, delivery.dropoff.longitude],
                option: { color: 'green', title: 'Drop off' }
            }
        ]

        setCurrentLocation([delivery.dropoff.latitude, delivery.dropoff.longitude])
        return locationPoints;
    };

    return (
        <div>
            <h2>Drone Tracking:</h2>

            <div style={{ paddingBottom: 10 }}>
                <input type="text"
                    style={{ marginRight: 10, width: '400px', border: '2px solid #008CBA' }}
                    onChange={handleInput} placeholder="Enter tracking id"></input>

                <button type="primary" className="main-button" onClick={onTrack}>Track</button>
                {showWarning && <span style={{ paddingLeft: 10,color:'red' }}>{warning}</span>}
            </div>
            <div style={{ height: "600px", width: "1000px" }}>
                <ReactBingmaps
                    disableStreetside={true}
                    zoom={12}
                    navigationBarMode={"compact"}
                    bingmapKey="ApNNsibpeT5vu3CzJDsU2qX755x7lF8N-tlrSUGc9iaUthHe0HcMzcX1B2yHYzec"
                    center={currentLocation}
                    pushPins={droneLocationPoints}
                >
                </ReactBingmaps>
            </div>
        </div>
    );
}
