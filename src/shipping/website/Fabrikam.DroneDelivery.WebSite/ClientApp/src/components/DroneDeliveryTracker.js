import React, { useState,useEffect } from 'react';
import { ReactBingmaps } from 'react-bingmaps';
import {droneDeliveryService } from '../services/DroneDeliveryService'
export const  DroneDeliveryTracker = ()=> {
    let locationPoints = [
        {
            location: ["11.874477", "75.370369"],
            option: { color: 'red', title: 'Pickup lcoation'}
       
        },
        {
            location: ["11.258753", "75.780411"],
            option: { color: 'green', title: 'Current location' }

        },
        {
            location: ["10.051969", "76.315773"],
            option: { color: 'yellow', title: 'Dropoff location ' }

        },
    ];
    const [droneLocationPoints, setDroneLocations] = useState(locationPoints);
    const [trackingId,setTrackingId] = useState(1);

    useEffect(() => {
        (async () => {
            const locations = await droneDeliveryService.fetchCompleteTrackingInfo(trackingId);
           setDroneLocations(locations);
        })();
    }, []);

    return (
          <div>
            <h1> Drone delivery tracker:</h1>
            <div style={{paddingBottom:10}}>
            <input type="text" placeholder="Enter tracking id"></input>
            <button type="primary" >Track</button>
            </div>
            <div style={{ height: "600px", width: "1000px" } }>
                <ReactBingmaps
                    disableStreetside={true}
                    navigationBarMode={"compact"}
                    bingmapKey="ApNNsibpeT5vu3CzJDsU2qX755x7lF8N-tlrSUGc9iaUthHe0HcMzcX1B2yHYzec"
                    center={[11.874477, 75.370369]}
                    pushPins={droneLocationPoints}>
                </ReactBingmaps>
            </div>      
      </div>
    );
}
