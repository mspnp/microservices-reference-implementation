import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import DroneDeliveryService from '../services/DroneDeliveryService';

export const DroneDeliveryTracker = () => {
    const [bingMapKey, setBingmapKey] = useState('');
    const [droneLocationPoints, setDroneLocations] = useState([]);
    const [trackingId, setTrackingId] = useState('');
    const [currentLocation, setCurrentLocation] = useState([]);
    const [showWarning, setShowWarning] = useState(false);
    const [warning, setWarning] = useState('');

    useEffect(() => {
        const fetchBingmapKey = async () => {
            localStorage.clear();
            const droneDeliveryService = new DroneDeliveryService();
            const bingMapKey = localStorage.getItem('bingMapKey') || '';
            if (bingMapKey) {
                setBingmapKey(bingMapKey);
            } else {
                const bingMapKey = await droneDeliveryService.getBingMapKey();
                localStorage.setItem('bingMapKey', bingMapKey);
                setBingmapKey(bingMapKey);
            }
        }
        fetchBingmapKey();
    }, [])

    const onTrack = async () => {
        if (!trackingId) {
            setShowWarning(true);
            setWarning("Input tracking id !!");
            return;
        }
        const droneDeliveryService = new DroneDeliveryService();
        let delivery;
        let droneLocation;
        try {
            delivery = await droneDeliveryService.getDelivery(trackingId);
            droneLocation = await droneDeliveryService.getDroneLocation(trackingId);
            if (delivery.id) {
                const locationPoints = populateLocations(delivery, droneLocation);
                setDroneLocations(locationPoints);
                setShowWarning(false);
            } else {
                setShowWarning(true);
                setWarning('No data available for given tracking id !!');
                setDroneLocations([])
            }
        } catch (error) {
            setShowWarning(true);
            setWarning("Request can not be processed!!");
        }
    }


    const handleInput = (event) => {
        const trackingid = event.target.value;
        if (!trackingid) {
            setShowWarning(true);
            setWarning("Input tracking id !!");
        } else {
            setShowWarning(false);
        }
        setTrackingId(trackingid)
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
                    className={showWarning ? 'custom-input error' : 'custom-input'}
                    onChange={handleInput} placeholder="Enter tracking id"></input>
                <button type="primary" className="main-button" onClick={onTrack}>Track</button>
                {showWarning && <span style={{ paddingLeft: 10, color: 'red' }}>{warning}</span>}
            </div>
            <div style={{ height: "600px", width: "1000px" }}>
                {bingMapKey && <ReactBingmaps
                    disableStreetside={true}
                    zoom={12}
                    navigationBarMode={"compact"}
                    bingmapKey={bingMapKey}
                    center={currentLocation}
                    pushPins={droneLocationPoints}
                >
                </ReactBingmaps>}
            </div>
        </div>
    );
}
