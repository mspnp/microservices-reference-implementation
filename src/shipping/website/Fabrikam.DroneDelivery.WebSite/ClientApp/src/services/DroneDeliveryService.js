import {
    axiosInstance
} from '../axiosHelper';
export default class DroneDeliveryService {

    getDelivery = async (trackingId) => {
        const response = await axiosInstance.get(`api/drone/${trackingId}`);
        return response.data;
    }

    getDroneLocation = async (trackingId) => {
        const response = await axiosInstance.get(`api/drone/${trackingId}/dronelocation`);
        return response.data;
    }
    deliveryRequest = async (requestData) => {
        const response = await axiosInstance.post('api/drone/deliveryrequest', requestData);
        return response.data;
    }
    getBingMapKey = async () => {
        const response = await axiosInstance.get('api/drone/bingMapKey');
        return response.data;
    }
}