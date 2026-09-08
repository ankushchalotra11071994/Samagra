import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./pages/Login";
import Products from './pages/Products';

// Routes के अंदर

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<Login />} />
        <Route path="/products" element={<Products />} />
        <Route path="/dashboard" element={<div className="p-8">Dashboard</div>} />
      </Routes>
    </BrowserRouter>
  );
}