'use client';

import React, { useState, useEffect } from "react";
import AuthContext, { type User } from "./authContext";

export default function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | undefined>(undefined);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check the cookie's validity by hitting the local BFF endpoint
   checkAuthStatus();
  }, []);

  const checkAuthStatus = async () => {
    setIsLoading(true);
    try {
      const res = await fetch("/bff/user/status");
      const data = await res.json();
      if (data.isAuthenticated) {
        setUser(data);
        setIsAuthenticated(true);
      } else {
        setIsAuthenticated(false);
        setUser(undefined);
      }
    } catch (err) {
      console.error("Auth status check failed", err);
    } finally {
      setIsLoading(false);
    }
  };

  function signOut() {
    // Clear the cookie by hitting the local BFF endpoint
    fetch("/bff/user/signout", { method: "POST" })
      .then(() => {
        setIsAuthenticated(false);    
    setUser(undefined);
      })
      .catch((err) => console.error("Sign out failed", err));
  }

  function login() {
    // Redirect to the local BFF endpoint to initiate the login flow
    window.location.href = "/bff/user/signin";
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        loadingUserData: isLoading,
        authError: null,
        checkAuthStatus,
        login,
        signOut,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
