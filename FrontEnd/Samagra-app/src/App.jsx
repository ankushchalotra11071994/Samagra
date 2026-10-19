import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';

import { AuthProvider } from './context/AuthContext';
import { CartProvider } from './context/CartContext';
import RequireAuth from './components/RequireAuth';
import ShopLayout from './components/ShopLayout';
import AdminLayout from './components/AdminLayout';

// Public
import Login from './pages/Login';
import RegisterPage from './pages/RegisterPage';

// Buyer screens
import HomePage from './pages/HomePage';
import ProductDetailPage from './pages/ProductDetailPage';
import CartPage from './pages/CartPage';
import OrderPage from './pages/OrderPage';
import PaymentSuccessPage from './pages/PaymentSuccessPage';
import MyOrdersPage from './pages/MyOrdersPage';
import OrderDetailPage from './pages/OrderDetailPage';
import ProfilePage from './pages/ProfilePage';
import AiChat from './components/AiChat';
import HooksPractice from './components/HooksPractice';
import NotFoundPage from './pages/NotFoundPage';

// Admin screens
import AdminDashboardPage from './pages/admin/AdminDashboardPage';
import AdminProductsPage from './pages/admin/AdminProductsPage';
import AdminProductFormPage from './pages/admin/AdminProductFormPage';
import AdminCategoriesPage from './pages/admin/AdminCategoriesPage';
import AdminOrdersPage from './pages/admin/AdminOrdersPage';
import AdminOrderDetailPage from './pages/admin/AdminOrderDetailPage';
import AdminAiUsagePage from './pages/admin/AdminAiUsagePage';
import BulkImportPage from './pages/BulkImportPage';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <CartProvider>
          <Routes>
            {/* Public */}
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<RegisterPage />} />

            {/* Signed-in buyer screens — common header */}
            <Route element={<RequireAuth />}>
              <Route element={<ShopLayout />}>
                <Route path="/" element={<Navigate to="/home" replace />} />
                <Route path="/home" element={<HomePage />} />
                <Route path="/products/:id" element={<ProductDetailPage />} />
                <Route path="/cart" element={<CartPage />} />
                <Route path="/order" element={<OrderPage />} />
                <Route path="/payment-success/:id" element={<PaymentSuccessPage />} />
                <Route path="/orders" element={<MyOrdersPage />} />
                <Route path="/orders/:id" element={<OrderDetailPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route path="/ai" element={<AiChat />} />
                <Route path="/hooks" element={<HooksPractice />} />
              </Route>

              {/* Admin screens — sidebar shell, Admin role only */}
              <Route element={<RequireAuth requireAdmin />}>
                <Route path="/admin" element={<AdminLayout />}>
                  <Route index element={<AdminDashboardPage />} />
                  <Route path="products" element={<AdminProductsPage />} />
                  <Route path="products/new" element={<AdminProductFormPage />} />
                  <Route path="products/:id/edit" element={<AdminProductFormPage />} />
                  <Route path="categories" element={<AdminCategoriesPage />} />
                  <Route path="orders" element={<AdminOrdersPage />} />
                  <Route path="orders/:id" element={<AdminOrderDetailPage />} />
                  <Route path="bulk-import" element={<BulkImportPage />} />
                  <Route path="ai-usage" element={<AdminAiUsagePage />} />
                </Route>
              </Route>
            </Route>

            {/* Bulk import admin ke neeche chala gaya — purana link kaam karta rahe */}
            <Route path="/bulk-import" element={<Navigate to="/admin/bulk-import" replace />} />
            <Route path="/Home" element={<Navigate to="/home" replace />} />

            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </CartProvider>
      </AuthProvider>
    </BrowserRouter>
  );
}
