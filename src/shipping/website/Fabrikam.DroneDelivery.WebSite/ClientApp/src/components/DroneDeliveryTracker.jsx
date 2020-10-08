import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import { droneDeliveryService } from '../services/DroneDeliveryService';

export const DroneDeliveryTracker = () => {
    const [droneLocationPoints, setDroneLocations] = useState([]);
    const [trackingId, setTrackingId] = useState();
    const [currentLocation, setCurrentLocation] = useState([]);

    const onTrack = async () => {
        const info = await droneDeliveryService.fetchCompleteTrackingInfo(trackingId);
        const locationPoints = populateLocations(info);
        setDroneLocations(locationPoints);
    }
    const handleInput = (event) => {
        setTrackingId(event.target.value)
    }

    const populateLocations = (info) => {

        let locationPoints = [
            {
                location: [info.pickup.latitude, info.pickup.longitude],
                option: { color: 'red', title: 'Pick up' },
            },
            {
                location: [info.currentLocation.latitude, info.currentLocation.longitude],
                option: { color: 'green', title: 'Current location' }
            },
            {
                location: [info.dropoff.latitude, info.dropoff.longitude],
                option: { color: 'green', title: 'Drop off' }
            }
        ]

        setCurrentLocation([info.dropoff.latitude, info.dropoff.longitude])
        return locationPoints;
    };
    return (
        <div>
            <h1> Drone delivery tracker:</h1>
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
            </div>
            <div style={{ height: "600px", width: "1000px" }}>
                <ReactBingmaps
                    disableStreetside={true}
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
