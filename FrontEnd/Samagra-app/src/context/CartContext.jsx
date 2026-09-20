// src/context/CartContext.jsx

import { createContext, useContext, useState } from 'react';

const CartContext = createContext();

export function CartProvider({ children }) {
  const [cartItems, setCartItems] = useState([]);

  // Cart mein item add karo
  const addToCart = (product) => {
    setCartItems((prev) => {
      // Pehle se hai toh quantity badhao
      const exists = prev.find((item) => item.id === product.id);
      if (exists) {
        return prev.map((item) =>
          item.id === product.id
            ? { ...item, quantity: item.quantity + 1 }
            : item
        );
      }
      // Nahi hai toh naya add karo
      return [...prev, { ...product, quantity: 1 }];
    });
  };

  // Cart se item remove karo
  const removeFromCart = (productId) => {
    setCartItems((prev) => prev.filter((item) => item.id !== productId));
  };

  // Quantity update karo
  const updateQuantity = (productId, quantity) => {
    if (quantity <= 0) {
      removeFromCart(productId);
      return;
    }
    setCartItems((prev) =>
      prev.map((item) =>
        item.id === productId ? { ...item, quantity } : item
      )
    );
  };

  // Total calculate karo
  const totalAmount = cartItems.reduce(
    (sum, item) => sum + item.price * item.quantity, 0
  );

  // Total items count
  const totalItems = cartItems.reduce(
    (sum, item) => sum + item.quantity, 0
  );

  // Cart clear karo — order ke baad
  const clearCart = () => setCartItems([]);

  return (
    <CartContext.Provider value={{
      cartItems,
      addToCart,
      removeFromCart,
      updateQuantity,
      totalAmount,
      totalItems,
      clearCart
    }}>
      {children}
    </CartContext.Provider>
  );
}

// Custom hook — easy use ke liye
export const useCart = () => useContext(CartContext);