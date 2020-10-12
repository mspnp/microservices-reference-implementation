import axios from 'axios';

const axiosInstance = axios.create({
    headers: {
        'X-Requested-With': 'XMLHttpRequest',
        'Content-Type': 'application/json',
    },
});

export { axiosInstance };
