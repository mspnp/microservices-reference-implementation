export default class DroneDeliveryService {

    getDelivery = async (trackingId) => {
        const response = await fetch(`api/drone/${trackingId}`);
        return response.json();
    }

    getDroneLocation = async (trackingId) => {
        const response = await fetch(`api/drone/${trackingId}/dronelocation`);
        return response.json();
    }
    deliveryRequest = async (requestData) => {
        const params = {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData),
        }
        const response = await fetch('api/drone/deliveryrequest',params);
        return response.json();
    }
}
