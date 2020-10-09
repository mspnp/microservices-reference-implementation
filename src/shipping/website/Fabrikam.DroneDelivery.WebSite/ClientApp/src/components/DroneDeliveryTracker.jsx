import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import DroneDeliveryService  from '../services/DroneDeliveryService';

export const DroneDeliveryTracker = () => {
    const [droneLocationPoints, setDroneLocations] = useState([]);
    const [trackingId, setTrackingId] = useState();
    const [currentLocation, setCurrentLocation] = useState([]);
    const [showWarning, setShowWarning] = useState(false);

    const onTrack = async () => {
        const droneDeliveryService = new DroneDeliveryService();

        const delivery = await droneDeliveryService.getDelivery(trackingId);
        const droneLocation = await droneDeliveryService.getDroneLocation(trackingId);

        if (delivery && delivery.id) {
            const locationPoints = populateLocations(delivery, droneLocation);

            setDroneLocations(locationPoints); 
            setShowWarning(false);
        } else {
            setShowWarning(true);
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
                location: [droneLocation.latitude, droneLocation.longitude],
                option: { color: 'blue', title: 'Drone', icon: 'https://squalldronestorage.blob.core.windows.net/images/mapdrone.png' }
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

                <button type="primary"
                    style={{
                        width: '100px',
                        backgroundColor: 'white',
                        color: 'black',
                        border: '2px solid #008CBA',
                        borderRadius: '12px',
                        fontSize: '16px',
                        margin: '4px 2px',
                        cursor: 'pointer',
                        padding: '5px 10px',
                        textAlign: 'center',
                        textDecoration: 'none',
                        display: 'inline-block',
                    }}
                    onClick={onTrack}>Track</button>

                {showWarning && <span>No data available</span>}
            </div>
            <div style={{ height: "600px", width: "1000px" }}>
                <ReactBingmaps
                    disableStreetside={true}
                    zoom={8}
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
