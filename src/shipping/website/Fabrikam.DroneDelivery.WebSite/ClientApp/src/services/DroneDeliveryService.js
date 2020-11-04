import {
    axiosInstance
} from '../axiosHelper';
export default class DroneDeliveryService {
    getDelivery = async (trackingId) => {
        const response = await axiosInstance.get(`api/DroneSite/${trackingId}`);
        return response.data;
    }
    getDroneLocation = async (trackingId) => {
        const response = await axiosInstance.get(`api/DroneSite/${trackingId}/dronelocation`);
        return response.data;
    }
    deliveryRequest = async (requestData) => {
        const response = await axiosInstance.post('api/DroneSite/deliveryrequest', requestData);
        return response.data;
    }
    getBingMapKey = async () => {
        const response = await axiosInstance.get('api/DroneSite/bingMapKey');
        return response.data;
    }
}