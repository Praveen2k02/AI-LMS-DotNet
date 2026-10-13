import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState,
} from "react";

import {
    api,
    clearToken,
    readToken,
    saveToken,
} from "../api/client";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
    const [user, setUser] = useState(null);
    const [ready, setReady] = useState(false);

    // ---------------------------------------------------------
    // Load logged-in user from existing JWT token
    // ---------------------------------------------------------
    const loadUser = useCallback(async () => {
        const token = readToken();

        // No token = user is not logged in
        if (!token) {
            setUser(null);
            setReady(true);
            return;
        }

        try {
            // Backend endpoint:
            // GET /api/auth/me
            const { data } = await api.get("/auth/me");

            // Supports both:
            // { user: {...} }
            // and directly returned user object
            const currentUser = data?.user ?? data;

            setUser(currentUser);
        } catch (error) {
            console.error("Failed to load current user:", error);

            // Invalid/expired token
            clearToken();
            setUser(null);
        } finally {
            setReady(true);
        }
    }, []);

    // ---------------------------------------------------------
    // Run when application starts
    // ---------------------------------------------------------
    useEffect(() => {
        loadUser();

        // Listen for token expiration from API client
        const onExpired = () => {
            clearToken();
            setUser(null);
            setReady(true);
        };

        window.addEventListener("ailms:auth-expired", onExpired);

        return () => {
            window.removeEventListener("ailms:auth-expired", onExpired);
        };
    }, [loadUser]);

    // ---------------------------------------------------------
    // LOGIN
    // ---------------------------------------------------------
    const login = useCallback(
        async (email, password, remember = false) => {
            try {
                const { data } = await api.post("/auth/login", {
                    email,
                    password,
                });

                // Backend should return:
                // {
                //   token: "...",
                //   user: {
                //      id: "...",
                //      name: "...",
                //      email: "...",
                //      role: "Student"
                //   }
                // }

                if (!data?.token) {
                    throw new Error("Login succeeded but no authentication token was returned.");
                }

                saveToken(data.token, remember);

                const loggedInUser = data?.user ?? null;

                setUser(loggedInUser);
                setReady(true);

                return loggedInUser;
            } catch (error) {
                console.error("Login failed:", error);
                throw error;
            }
        },
        []
    );

    // ---------------------------------------------------------
    // REGISTER
    // ---------------------------------------------------------
    const register = useCallback(async (form) => {
        try {
            const { data } = await api.post("/auth/register", form);

            if (!data?.token) {
                throw new Error(
                    "Registration succeeded but no authentication token was returned."
                );
            }

            // Registration keeps the user logged in
            saveToken(data.token, true);

            const registeredUser = data?.user ?? null;

            setUser(registeredUser);
            setReady(true);

            return registeredUser;
        } catch (error) {
            console.error("Registration failed:", error);
            throw error;
        }
    }, []);

    // ---------------------------------------------------------
    // LOGOUT
    // ---------------------------------------------------------
    const logout = useCallback(async () => {
        try {
            // If backend has /auth/logout, call it.
            // If not, local logout will still happen.
            await api.post("/auth/logout");
        } catch (error) {
            // JWT authentication is normally stateless.
            // Removing the token is enough for frontend logout.
            console.warn(
                "Backend logout request failed. Clearing local authentication anyway."
            );
        } finally {
            clearToken();
            setUser(null);
            setReady(true);
        }
    }, []);

    // ---------------------------------------------------------
    // Reload authenticated user
    // ---------------------------------------------------------
    const reload = useCallback(async () => {
        await loadUser();
    }, [loadUser]);

    // ---------------------------------------------------------
    // Check whether user is authenticated
    // ---------------------------------------------------------
    const isAuthenticated = !!user && !!readToken();

    // ---------------------------------------------------------
    // Get current user's role
    // ---------------------------------------------------------
    const role = user?.role ?? null;

    // ---------------------------------------------------------
    // Context value
    // ---------------------------------------------------------
    const value = useMemo(
        () => ({
            user,
            ready,
            isAuthenticated,
            role,

            login,
            register,
            logout,

            reload,
        }),
        [
            user,
            ready,
            isAuthenticated,
            role,
            login,
            register,
            logout,
            reload,
        ]
    );

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

// ---------------------------------------------------------
// useAuth Hook
// ---------------------------------------------------------
export function useAuth() {
    const value = useContext(AuthContext);

    if (!value) {
        throw new Error("useAuth must be used within AuthProvider");
    }

    return value;
}