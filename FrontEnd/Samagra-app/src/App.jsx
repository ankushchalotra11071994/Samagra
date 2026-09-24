import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./pages/Login"; 
import HomePage from './pages/HomePage';
import ProductDetailPage from './pages/ProductDetailPage';
import CartPage from './pages/CartPage';
import OrderPage from './pages/OrderPage';
import PaymentSuccessPage from './pages/PaymentSuccessPage';
import AiChat from './components/AiChat';

// routes में
<Route path="/ai" element={<AiChat />} />
// Routes के अंदर

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<Login />} />
         <Route path="/Home" element={<HomePage />} /> 
         <Route path="/cart" element={<CartPage />} />
         <Route path="/order" element={<OrderPage />} />
         <Route path="/payment-success/:id" element={<PaymentSuccessPage />} />
          <Route path="/products/:id"    element={<ProductDetailPage />} /> 
          <Route path="/ai" element={<AiChat />} />
      </Routes>
    </BrowserRouter>
  );
}