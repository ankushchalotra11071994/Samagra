// src/services/bulkImportService.js

import axios from 'axios';
import api from '../api/axios';

// Step 1 — backend se job + SAS upload URL lo
export const createUploadUrl = async (file) => {
  const response = await api.post('/bulkimports/upload-url', {
    fileName: file.name,
    fileSizeBytes: file.size,
  });
  return response.data; // { jobId, uploadUrl, expiresAt }
};

// Step 2 — file seedha Blob Storage pe PUT karo (backend se hokar nahi jaati)
// Shared `api` instance nahi — uska baseURL aur cookies blob ke liye galat hain
export const uploadToBlob = async (uploadUrl, file, onProgress) => {
  await axios.put(uploadUrl, file, {
    headers: {
      'x-ms-blob-type': 'BlockBlob',
      'Content-Type': file.type || 'text/csv',
    },
    onUploadProgress: (e) => {
      if (e.total) onProgress?.(Math.round((e.loaded * 100) / e.total));
    },
  });
};

// Step 3 — backend ko batao upload ho gaya, wo Service Bus pe message bhejega
export const startImport = async (jobId) => {
  const response = await api.post(`/bulkimports/${jobId}/start`);
  return response.data;
};

export const getImportJob = async (jobId) => {
  const response = await api.get(`/bulkimports/${jobId}`);
  return response.data;
};

export const getRecentImports = async () => {
  const response = await api.get('/bulkimports');
  return response.data;
};

export const getImportErrors = async (jobId) => {
  const response = await api.get(`/bulkimports/${jobId}/errors`);
  return response.data; // [{ rowNumber, message }]
};
