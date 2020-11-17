import React, { useState, useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import DroneDeliveryService from '../services/DroneDeliveryService';
import ConfigurationService from '../services/ConfigurationService';
import * as signalR from '@microsoft/signalr';
import { css } from "@emotion/core";
import ScaleLoader from "react-spinners/ScaleLoader"

export const DroneDeliveryTracker = () => {

    const droneErrorIconurl = 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAACXBIWXMAAAsTAAALEwEAmpwYAAACbUlEQVRIibWVPW/TUBSG3+tey1FKF2fCLTSiiYlaihSpQ5HgL6QZW6RkYGBgowMs2RASQqh0YWOhA4ilH/kXhapFKolK7AhQkTMlS0hIZMeXwR+pk5smadV3snWP3+eec+71Aa5Y5LzF/elERKRWmhCSAsM8gOvuUgUERcZYXupIOwt/CrWxAAeKEqbi5DoDngGYGrLJOgFeW2ZjY8kwmkMBx3NzNyxL2AOQHGLcqyNK7ZXFcvl0IMA130e3FOPKoNRePgsRvIcDRQm7O7+oOQAoliXsHShKuA9Axcl1jF8WnpIT4uTTAGB/OhFxGxrQzTevEPu0BRqJ9LlQWYaa30Z8+zMP8rwwsyD7AJFaaXBOixSN4tq9ZQciywHz2McthO/egRAK8QBT5kQ77QMIISle1M/HT9A6KSGkxn2IZx5KqGhpOsqZR7xPwZiQAtxTdDQb1wDEeIEBw5MSAPjm+moGVrXKBYBAS/7SVK/JA0+OVatBf5h1Mkmoo5kDAHM8hcER54ixkUM9QGVQQKBEJQ0tTQ/0ZKCI4ym4L8VRzPW1LPTVzGgQmxR9AGMsz4u59eG9Y/6j5Nfcqla7kNsqou82+QkQO+8DpI60A6DeG8RabTSPv0Nfy8Kqdf/IHuTvl6/4556sHtXFjrTrFMfVt9l4jgEv+PmOrVzyt/bSzwAALLOxAeDoss4MOOyYjbfeuw9YMowmpfYKAOMS/oZI7fTZwRO4B4vl8iml9jIukAkDDntnQR/Ag3TMxn0AOXAaz1EdQM42Gw96zYEhQ78wsyCbE+00Y0IKApv3rj8IKrBJkRA7L3ak3fOG/pXrP2gABsQqGCMbAAAAAElFTkSuQmCC';
    const droneIconUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACMAAAAjCAYAAAAe2bNZAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAIGNIUk0AAHolAACAgwAA+f8AAIDpAAB1MAAA6mAAADqYAAAXb5JfxUYAAAJ2SURBVHja7NdNiI1hFAfw38xcZgwLk698ZiMLXwn5miQbUUixpFnJQiJKSfKdj1KSjdmgxIJSyEKyQNnJArORj4Z8lQgzzIxrc269vb333rlmZBb31O2873nP8zz/5znnf85za/L5vIEitQaQVMFUwVQqNTZl2s/gBm79gzXXYgm2Z4HZgPUYgW78xhy8Qzvq0IhrOPwXi+/HCnSgB5MwEo8iMoPwFpdz6ArHzgDTjV9h60AONfj5lyfRmfh1h/6B7wGkJ547i4XpIS7idMkQMwvjErY3eFwG3C4sw/L0h1yG80rMx3CcLXIidTiRFXccw+4Id1qGYTMmoxn3y4FpCD0U4/E14fcbM3AIC0rsfDH24FmCsd2Rl43xXt+bkyk0qwl4kngvhKY+dClpxt041fT4htQ6JcEUpAefE4PyEZ5RoctJsfGji9W3XJF8gBeYG4wq2DqwDpfLAOrAGtzGkLB1xUYeYUwWoCwwT3E1QvQl9a0pWFDuZBqCCA+CxgX5hAuYiOe9rcBZMgE3MRNX8Ao7MvwOY3YCzKoIV1nJ9RLIYFwKIPuiqtYGQxZFotZHWA7GmJNB/fNY3ZfelEXXoziFbRVU3/PYiE1o7Q8wTWiLpJwW+TA12FKHl9FbxkUxK9jboty34SOmR5vp0xViadCxNXrIzsiFh6Fbwq8lZd+C9ziHKSWKZEVgCpPcSVE/eXKzQ2fNfTv0wv5I4LGhr+MIvqW+b4lE7UrZf2Avtqbm6ROYe5gXvSpfopcVqz3f8SHC12cwrSkmHO8lkxqjDByoXsj/B5iafvaruB0k5RVeZ7AqfaNrr3iX1T/+VTBVMP0kfwYA/DeTgr7tS7YAAAAASUVORK5CYII=";

    const [bingMapKey, setBingmapKey] = useState('');
    const [trackingId, setTrackingId] = useState('');
    const [buttonDisabled, setButtonDisabled] = useState(false);

    const [mapPoints, setMapPoints] = useState([]);
    const [droneLocation, setDroneLocation] = useState([]);
    const [mapCenterLocation, setMapCenterLocation] = useState([]);
    const [mapLocations, setMapLocations] = useState({});
    const [droneIcon, setDroneIcon] = useState(droneIconUrl);

    const [showWarning, setShowWarning] = useState(false);
    const [showError, setShowError] = useState(false);
    const [warning, setWarning] = useState('');

    const [droneStatus, setDroneStatus] = useState('None');
    const [droneAltitude, setDroneAltitude] = useState(0);
    const [loading, setLoading] = useState(false);
   
    const [hubConnection, setConnection] = useState();

    let droneLocationRetrieved = false;

    useEffect(() => {
        const fetchBingmapKey = async () => {
            const configurationService = new ConfigurationService();
            const bingMapKey = await configurationService.getBingMapKey();
            setBingmapKey(bingMapKey);
        }

        fetchBingmapKey();
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

    async function connect(deliveryId) {
        const configurationService = new ConfigurationService();
        const apiUrl = await configurationService.getApiUrl();

        if (hubConnection) {
            hubConnection.stop();
        }

        let connection = new signalR.HubConnectionBuilder()
            .withUrl(apiUrl + `/DroneHub`)
            .configureLogging(new MyLogger())
            .withAutomaticReconnect()
            .build();

        setConnection(connection);

        //Start signlR connection
        connection.start().then(resp => {
            console.log("Live tracking started")
            setButtonDisabled(false);
            setDroneIcon(droneIconUrl);
            setShowError(false);
            setShowWarning(false);
            setDroneLocation([]);

            connection.invoke("subscribe", deliveryId).catch(function (err) {
                return console.error(err.toString());
            });
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

            setDroneStatus(droneLocation.Stage);
            setDroneAltitude(droneLocation.Location.Altitude);
        });

        connection.onclose(function (e) {
            //if (!showError) {
            //    setShowError(false);
            //    setShowWarning(true);
            //    setWarning("Live tracking lost");
            //}
            //setDroneIcon(droneErrorIconurl);
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
            setLoading(true);

            const delivery = await droneDeliveryService.getDelivery(trackingId);
            if (delivery.id) {
                setMapCenterLocation([delivery.pickup.latitude, delivery.pickup.longitude]);

                setMapLocations({
                    Pickup: [delivery.pickup.latitude, delivery.pickup.longitude],
                    Dropoff: [delivery.dropoff.latitude, delivery.dropoff.longitude]
                });

                connect(delivery.id);

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
        } finally {
           setLoading(false);
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

    const override = css`
      display: block;
      margin: 0 auto;
      border-color: red;
      position:relative;
      top: -430px;
      left: 530px;
    `;

    return (
        <div>
            <div style={{ paddingBottom: 10 }}>
                <div>
                    <span style={{ width: '300px', display: 'inline-block', overflow: 'hidden' }}>
                        <a style={{ fontWeight: 'bold' }}>Delivery Status:</a><a style={{ paddingLeft: '10px' }}>{droneStatus}</a>
                    </span>
                    <span style={{ paddingLeft: '10px', display: 'inline-block', overflow: 'hidden' }}>
                        <a style={{ fontWeight: 'bold' }}>Drone Altitude (ft):</a><a style={{ paddingLeft: '10px' }}>{droneAltitude}</a>
                    </span>
                </div>
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
