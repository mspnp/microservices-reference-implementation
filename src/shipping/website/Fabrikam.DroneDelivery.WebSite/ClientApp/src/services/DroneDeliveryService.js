
export default class DroneDeliveryService {
    
    getDelivery = async (trackingId) => {
        const response = await fetch(`api/drone/${trackingId}`);
        return response.json();
    }

    getDroneLocation = async (trackingId) => {
        const response = await fetch(`api/drone/${trackingId}/dronelocation`);
        return response.json();
    }
}
