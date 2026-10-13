
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useAuth } from "./context/AuthContext";
import AppShell from "./components/AppShell";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import DashboardPage from "./pages/DashboardPage";
import CoursesPage from "./pages/CoursesPage";
import CourseDetailPage from "./pages/CourseDetailPage";
import PeoplePage from "./pages/PeoplePage";
import MarksPage from "./pages/MarksPage";
import CertificatesPage from "./pages/CertificatesPage";
import AssistantPage from "./pages/AssistantPage";
import VerifyPage from "./pages/VerifyPage";
import QuizPage from "./pages/QuizPage";
import { LoaderCircle } from "lucide-react";

function Protected({ children, roles }) {
    const { user, ready } = useAuth();
    const location = useLocation();

    if (!ready) {
        return (
            <div className="full-loader">
                <LoaderCircle className="spin" size={28} />
                <span>Loading your workspace…</span>
            </div>
        );
    }

    if (!user) {
        return (
            <Navigate
                to="/login"
                replace
                state={{ from: location.pathname }}
            />
        );
    }

    if (roles && !roles.includes(user.role)) {
        return <Navigate to="/dashboard" replace />;
    }

    return children;
}

function PrivatePage({ children, roles }) {
    return (
        <Protected>
            <AppShell>
                <Protected roles={roles}>{children}</Protected>
            </AppShell>
        </Protected>
    );
}

export default function App() {
    const { user, ready } = useAuth();

    return (
        <Routes>
            <Route
                path="/login"
                element={
                    ready && user ? (
                        <Navigate to="/dashboard" replace />
                    ) : (
                        <LoginPage />
                    )
                }
            />

            <Route
                path="/register"
                element={
                    ready && user ? (
                        <Navigate to="/dashboard" replace />
                    ) : (
                        <RegisterPage />
                    )
                }
            />

            <Route path="/verify" element={<VerifyPage />} />

            <Route
                path="/"
                element={
                    <Navigate
                        to={user ? "/dashboard" : "/login"}
                        replace
                    />
                }
            />

            <Route
                path="/dashboard"
                element={
                    <PrivatePage>
                        <DashboardPage />
                    </PrivatePage>
                }
            />

            <Route
                path="/courses"
                element={
                    <PrivatePage>
                        <CoursesPage />
                    </PrivatePage>
                }
            />

            <Route
                path="/courses/:id"
                element={
                    <PrivatePage>
                        <CourseDetailPage />
                    </PrivatePage>
                }
            />

            {/* Student quiz-taking page */}
            <Route
                path="/quizzes/:id"
                element={
                    <PrivatePage roles={["Student"]}>
                        <QuizPage />
                    </PrivatePage>
                }
            />

            <Route
                path="/students"
                element={
                    <PrivatePage roles={["Admin", "Trainer"]}>
                        <PeoplePage type="Student" />
                    </PrivatePage>
                }
            />

            <Route
                path="/trainers"
                element={
                    <PrivatePage roles={["Admin", "Trainer"]}>
                        <PeoplePage type="Trainer" />
                    </PrivatePage>
                }
            />

            <Route
                path="/admin/users"
                element={
                    <PrivatePage roles={["Admin"]}>
                        <PeoplePage type="All" />
                    </PrivatePage>
                }
            />

            <Route
                path="/marks"
                element={
                    <PrivatePage>
                        <MarksPage />
                    </PrivatePage>
                }
            />

            <Route
                path="/certificates"
                element={
                    <PrivatePage>
                        <CertificatesPage />
                    </PrivatePage>
                }
            />

            <Route
                path="/assistant"
                element={
                    <PrivatePage>
                        <AssistantPage />
                    </PrivatePage>
                }
            />

            <Route
                path="*"
                element={
                    <Navigate
                        to={user ? "/dashboard" : "/login"}
                        replace
                    />
                }
            />
        </Routes>
    );
}
