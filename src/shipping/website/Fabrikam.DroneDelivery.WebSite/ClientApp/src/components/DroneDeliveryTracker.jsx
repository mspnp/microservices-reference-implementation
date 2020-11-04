import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import DroneDeliveryService from '../services/DroneDeliveryService';
import ConfigurationService from '../services/ConfigurationService';
import * as signalR from '@microsoft/signalr';

export const DroneDeliveryTracker = () => {

    const droneErrorIconurl = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAACbUlEQVRIibWVPW/TUBSG3+tey1FKF2fCLTSiiYlaihSpQ5HgL6QZW6RkYGBgowMs2RASQqh0YWOhA4ilH/kXhapFKolK7AhQkTMlS0hIZMeXwR+pk5smadV3snWP3+eec+71Aa5Y5LzF/elERKRWmhCSAsM8gOvuUgUERcZYXupIOwt/CrWxAAeKEqbi5DoDngGYGrLJOgFeW2ZjY8kwmkMBx3NzNyxL2AOQHGLcqyNK7ZXFcvl0IMA130e3FOPKoNRePgsRvIcDRQm7O7+oOQAoliXsHShKuA9Axcl1jF8WnpIT4uTTAGB/OhFxGxrQzTevEPu0BRqJ9LlQWYaa30Z8+zMP8rwwsyD7AJFaaXBOixSN4tq9ZQciywHz2McthO/egRAK8QBT5kQ77QMIISle1M/HT9A6KSGkxn2IZx5KqGhpOsqZR7xPwZiQAtxTdDQb1wDEeIEBw5MSAPjm+moGVrXKBYBAS/7SVK/JA0+OVatBf5h1Mkmoo5kDAHM8hcER54ixkUM9QGVQQKBEJQ0tTQ/0ZKCI4ym4L8VRzPW1LPTVzGgQmxR9AGMsz4u59eG9Y/6j5Nfcqla7kNsqou82+QkQO+8DpI60A6DeG8RabTSPv0Nfy8Kqdf/IHuTvl6/4556sHtXFjrTrFMfVt9l4jgEv+PmOrVzyt/bSzwAALLOxAeDoss4MOOyYjbfeuw9YMowmpfYKAOMS/oZI7fTZwRO4B4vl8iml9jIukAkDDntnQR/Ag3TMxn0AOXAaz1EdQM42Gw96zYEhQ78wsyCbE+00Y0IKApv3rj8IKrBJkRA7L3ak3fOG/pXrP2gABsQqGCMbAAAAAElFTkSuQmCC';
    const droneIconUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAUCAYAAACNiR0NAAAACXBIWXMAAAsTAAALEwEAmpwYAAABJ0lEQVQ4jeXTyyrFURQG8B+OktxKRkQYMMBUJsoMI2XoGXgBL6AYKa9gJicTydjM3ES5RQZKR26hjsF/qe30P4dkIl99tfe6fHut1tr8e0z8IGc8vdRhCqtx78MZSpjBSxWRJuyiFb04D/tSGtSJnZzkMSwGx3L822jPe3UODxgOjmItqiwHn7GCkSTuDdPVBMu4Dz4lQpV8TOLKqWB9IljCEVqC7TjMefgAbUncReQiG0oeGrCFZtxhPuyb6MYVFqK6b2EZxaSDAfTHuRH7KiZaC424jko2Qng2WMQ6hmStVuvwE4ZwKVvYU1lbe8EyjmUf4AY9lckNOYIF2bK+YhIdGAzCLU5kg9iVbcO38VFhyuNaCfW1nD9B4Qv/KroqbNe/XcQfxzuZPEeYskKPRwAAAABJRU5ErkJggg==";

    const [bingMapKey, setBingmapKey] = useState('');
    const [trackingId, setTrackingId] = useState('');
    const [buttonDisabled, setButtonDisabled] = useState(true);

    const [mapPoints, setMapPoints] = useState([]);
    const [droneLocation, setDroneLocation] = useState([]);
    const [mapCenterLocation, setMapCenterLocation] = useState([]);
    const [mapLocations, setMapLocations] = useState({});
    const [droneIcon, setDroneIcon] = useState(droneIconUrl);

    const [showWarning, setShowWarning] = useState(false);
    const [showError, setShowError] = useState(false);
    const [warning, setWarning] = useState('');

    let connection;
    let droneLocationRetrieved = false;

    useEffect(() => {
        const fetchBingmapKey = async () => {
            const configurationService = new ConfigurationService();
            const bingMapKey = await configurationService.getBingMapKey();
            setBingmapKey(bingMapKey);
        }

        fetchBingmapKey();

        connect();
    }, [])

    useEffect(() => {
        populateLocations()
    }, [droneLocation])

    useEffect(() => {
        populateLocations()
    }, [mapLocations])

    useEffect(() => {
        populateLocations()
    }, [droneIcon])
 
    class MyLogger {
        log(logLevel, message) {
            if (logLevel >= 4) {
                if (message.indexOf('disconnect') > -1) {
                    if (droneLocationRetrieved) {
                        setShowError(true);
                        setShowWarning(false);
                        setWarning("Drone Location lost");
                    } else {
                        if (!showError) {
                            setShowError(false);
                            setShowWarning(true);
                            setWarning("Live tracking lost");
                        }
                    }
                    setDroneIcon(droneErrorIconurl);
                }
            }
        }
    }

    async function connect() {
        const configurationService = new ConfigurationService();
        const apiUrl = await configurationService.getApiUrl();

        connection = new signalR.HubConnectionBuilder()
            .withUrl(apiUrl + `/DroneHub`)
            .configureLogging(new MyLogger())
            .withAutomaticReconnect()
            .build();

        //Start signlR connection
        connection.start().then(resp => {
            console.log("Live tracking started")
            setButtonDisabled(false);
            setDroneIcon(droneIconUrl);
        }).catch(error => {
            if (!showError) {
                setShowError(false);
                setShowWarning(true);
                setWarning("Live tracking failed");
            }
            setDroneIcon(droneErrorIconurl);
        })

        // Listening to the drone live location
        connection.on("SendLocation", droneLocation => {
            console.log("SendLocation");
            droneLocationRetrieved = true;
            setDroneLocation
            (
                [
                    droneLocation.Location.Latitude,
                    droneLocation.Location.Longitude
                ]
            )
        });

        connection.onclose(function() {
            if (!showError) {
                setShowError(false);
                setShowWarning(true);
                setWarning("Live tracking lost");
            }
            setDroneIcon(droneErrorIconurl);
        });
    }

    const onTrack = async () => {
        if (!trackingId) {
            setShowWarning(true);
            setWarning("Input tracking id !!");
            return;
        }

        const droneDeliveryService = new DroneDeliveryService();
        try {
            const delivery = await droneDeliveryService.getDelivery(trackingId);
            if (delivery.id) {
                setMapCenterLocation([delivery.pickup.latitude, delivery.pickup.longitude]);

                setMapLocations({
                    Pickup: [delivery.pickup.latitude, delivery.pickup.longitude],
                    Dropoff: [delivery.dropoff.latitude, delivery.dropoff.longitude]
                });

                setShowError(false);
                setShowWarning(false);
            } else {
                setShowError(false);
                setShowWarning(true);
                setWarning('No data available for given tracking id !!');
                setMapPoints([])
            }

        } catch (error) {
            setShowError(false);
            setShowWarning(true);
            setWarning("Request can not be processed!! - " + error.message);
        }
    }

    const handleInput = (event) => {
        setShowError(false);
        const trackingid = event.target.value;
        if (!trackingid) {
            setShowWarning(true);
            setWarning("Input tracking id !!");
        } else {
            setShowWarning(false);
        }
        setTrackingId(trackingid)
    }

    const populateLocations = () => {
        let locationPoints = [
            {
                location: mapLocations.Pickup,
                option: { color: 'blue', title: 'Pick up' },
            },
            {
                location: droneLocation,
                option: { title: 'Drone', icon: droneIcon }
            },
            {
                location: mapLocations.Dropoff,
                option: { color: 'green', title: 'Drop off' }
            }
        ]
        setMapPoints(locationPoints);
    };

    return (
        <div>
            <div style={{ paddingBottom: 10 }}>
                <input type="text"
                    className={showWarning ? 'custom-input error' : 'custom-input'}
                    onChange={handleInput} placeholder="Enter tracking id"></input>

                <button type="primary" className="main-button" onClick={onTrack} disabled={buttonDisabled}>Track</button>
                {showWarning && <span className="warningBox">{warning}</span>}
                {showError && <span className="blink_me errorBox">{warning}</span>}
            </div>
            <div style={{ height: "700px", width: "1100px" }}>
                {bingMapKey &&
                    <ReactBingmaps
                        disableStreetside={true}
                        zoom={12}
                        navigationBarMode={"compact"}
                        bingmapKey={bingMapKey}
                        pushPins={mapPoints}
                        id="mainMap"
                        center={mapCenterLocation}
                    >
                    </ReactBingmaps>}
            </div>
        </div>
    );
}
