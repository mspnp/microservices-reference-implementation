
const createDroneDeliveryService = () => {

 const fetchCompleteTrackingInfo= async(trackngId) => {
    const response = await fetch(`locations/${trackngId}`);
    return response.json();
  };
  const fetchPickUpLocation= async(trackngId) => {
    const response = await fetch(`weatherforecast/${trackngId}`);
    return response.json();
  };
  const fetchCurrentLocation= async(trackngId) => {
    const response = await fetch(`weatherforecast/${trackngId}`);
    return response.json();
  };    
  const fetchDropOffLocation= async(trackngId) => {
    const response = await fetch(`weatherforecast/${trackngId}`);
    return response.json();
  };    
  return {
    fetchCompleteTrackingInfo,
    fetchPickUpLocation,
    fetchCurrentLocation,
    fetchDropOffLocation
};
};
export const droneDeliveryService = createDroneDeliveryService();
