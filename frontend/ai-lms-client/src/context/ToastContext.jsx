import {
    createContext,
    useCallback,
    useContext,
    useMemo,
    useState,
} from "react";

import {
    CheckCircle2,
    CircleAlert,
    Info,
    X,
} from "lucide-react";

const ToastContext = createContext(null);

// Toast icons
const icons = {
    success: CheckCircle2,
    error: CircleAlert,
    info: Info,
};

export function ToastProvider({ children }) {
    const [items, setItems] = useState([]);

    // ---------------------------------------------------------
    // Dismiss a toast
    // ---------------------------------------------------------
    const dismiss = useCallback((id) => {
        setItems((all) =>
            all.filter((item) => item.id !== id)
        );
    }, []);

    // ---------------------------------------------------------
    // Show a toast notification
    // ---------------------------------------------------------
    const notify = useCallback(
        (message, type = "success") => {
            const id = `${Date.now()}-${Math.random()}`;

            setItems((all) => [
                ...all.slice(-3),
                {
                    id,
                    message,
                    type,
                },
            ]);

            // Automatically remove after 4.2 seconds
            window.setTimeout(() => {
                dismiss(id);
            }, 4200);
        },
        [dismiss]
    );

    // ---------------------------------------------------------
    // Context value
    // ---------------------------------------------------------
    const value = useMemo(
        () => ({
            notify,
        }),
        [notify]
    );

    return (
        <ToastContext.Provider value={value}>
            {children}

            {/* Toast notifications */}
            <div
                className="toast-stack"
                aria-live="polite"
                aria-atomic="false"
            >
                {items.map((item) => {
                    const Icon = icons[item.type] || Info;

                    return (
                        <div
                            className={`toast toast-${item.type}`}
                            key={item.id}
                            role="status"
                        >
                            {/* Icon */}
                            <Icon size={18} />

                            {/* Message */}
                            <span>{item.message}</span>

                            {/* Close button */}
                            <button
                                type="button"
                                className="icon-btn toast-close"
                                onClick={() => dismiss(item.id)}
                                aria-label="Dismiss notification"
                            >
                                <X size={15} />
                            </button>
                        </div>
                    );
                })}
            </div>
        </ToastContext.Provider>
    );
}

// ---------------------------------------------------------
// useToast Hook
// ---------------------------------------------------------
export function useToast() {
    const value = useContext(ToastContext);

    if (!value) {
        throw new Error(
            "useToast must be used within ToastProvider"
        );
    }

    return value.notify;
}