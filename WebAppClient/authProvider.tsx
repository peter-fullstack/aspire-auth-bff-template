import React, { useState, useEffect } from "react";
import AuthContext, { type User } from "./authContext";

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | undefined>(undefined);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Check the cookie's validity by hitting the local BFF endpoint
    fetch("/bff/user/status")
      .then((res) => res.json())
      .then((data) => {
        if (data.isAuthenticated) {
          setUser(data);
          setIsAuthenticated(true);
        } else {
          setIsAuthenticated(false);
          setUser(undefined);
        }
      })
      .catch((err) => console.error("Auth status check failed", err))
      .finally(() => setIsLoading(false));
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

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        loadingUserData: isLoading,
        authError: null,
        checkAuthStatus,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
