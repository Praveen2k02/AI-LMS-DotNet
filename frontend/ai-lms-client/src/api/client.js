import axios from "axios";

export const API_BASE_URL =
    import.meta.env.VITE_API_URL || "http://localhost:5080/api";

export const api = axios.create({
    baseURL: API_BASE_URL,
    timeout: 120000, // 120 seconds for Ollama AI responses
});

export function readToken() {
    return (
        localStorage.getItem("ailms.token") ||
        sessionStorage.getItem("ailms.token")
    );
}

export function saveToken(token, remember = false) {
    localStorage.removeItem("ailms.token");
    sessionStorage.removeItem("ailms.token");

    (remember ? localStorage : sessionStorage).setItem(
        "ailms.token",
        token
    );
}

export function clearToken() {
    localStorage.removeItem("ailms.token");
    sessionStorage.removeItem("ailms.token");
}

api.interceptors.request.use((config) => {
    const token = readToken();

    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
});

api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response?.status === 401) {
            clearToken();
            window.dispatchEvent(new CustomEvent("ailms:auth-expired"));
        }

        const data = error.response?.data;

        error.userMessage =
            data?.message ||
            data?.title ||
            error.message ||
            "Request failed. Please try again.";

        return Promise.reject(error);
    }
);

export function errorMessage(error) {
    return (
        error?.userMessage ||
        error?.response?.data?.message ||
        error?.message ||
        "Something went wrong."
    );
}