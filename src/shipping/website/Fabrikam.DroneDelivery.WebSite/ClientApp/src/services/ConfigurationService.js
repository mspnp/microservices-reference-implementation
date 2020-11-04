import {
    axiosInstance
} from '../axiosHelper';
export default class ConfigurationService {
    getBingMapKey = async () => {
        const response = await axiosInstance.get('api/Configuration/bingMapKey');
        return response.data;
    }
    getApiUrl= async () => {
        const response = await axiosInstance.get('api/Configuration/apiUrl');
        return response.data;
    }
}