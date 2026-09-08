import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5109/api',
  withCredentials: true,   // ← cookies भेजने/लेने के लिए ज़रूरी
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api;