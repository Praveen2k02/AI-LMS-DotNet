import { useMemo, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import {
  Activity, Award, BookOpen, ChartNoAxesCombined, ChevronDown, CircleHelp, ClipboardCheck,
  GraduationCap, LayoutDashboard, LogOut, Menu, MessageCircle, Search, Settings2, ShieldCheck,
  Sparkles, Users, X, UserRound, Layers3
} from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const navFor = {
  Admin: [
    { section: "WORKSPACE", items: [["Overview", "/dashboard", LayoutDashboard], ["Courses", "/courses", BookOpen], ["People", "/admin/users", Users]] },
    { section: "ACADEMY", items: [["Students", "/students", GraduationCap], ["Trainers", "/trainers", UserRound], ["Marks & reports", "/marks", ChartNoAxesCombined], ["Certificates", "/certificates", Award]] },
    { section: "AI LEARNING", items: [["AI Study Assistant", "/assistant", Sparkles]] },
  ],
  Trainer: [
    { section: "TEACHING", items: [["Overview", "/dashboard", LayoutDashboard], ["My courses", "/courses", BookOpen], ["Students", "/students", Users], ["Marks & reports", "/marks", ChartNoAxesCombined]] },
    { section: "TOOLS", items: [["AI Study Assistant", "/assistant", Sparkles], ["Certificates", "/certificates", Award]] },
  ],
  Student: [
    { section: "MY LEARNING", items: [["My dashboard", "/dashboard", LayoutDashboard], ["Browse courses", "/courses", BookOpen], ["Marks", "/marks", ChartNoAxesCombined], ["Certificates", "/certificates", Award]] },
    { section: "STUDY TOOLS", items: [["AI Study Assistant", "/assistant", Sparkles]] },
  ],
};

const titles = {
  "/dashboard": ["Dashboard", "Your learning, at a glance"],
  "/courses": ["Courses", "Explore courses and keep learning"],
  "/students": ["Students", "Manage enrolments and learner progress"],
  "/trainers": ["Trainers", "Meet your teaching team"],
  "/admin/users": ["People", "Manage users, trainers and students"],
  "/marks": ["Marks & reports", "Results and assessment records"],
  "/certificates": ["Certificates", "Your achievements and credentials"],
  "/assistant": ["AI Study Assistant", "A little help goes a long way"],
};

export default function AppShell({ children }) {
  const { user, logout } = useAuth();
  const notify = useToast();
  const location = useLocation();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const menu = navFor[user?.role] || navFor.Student;
  const [title, subtitle] = useMemo(() => {
    if (location.pathname.startsWith("/courses/")) return ["Course workspace", "Lessons, assignments, quizzes and AI tools"];
    return titles[location.pathname] || ["AI-LMS", "Learning that moves you forward"];
  }, [location.pathname]);

  async function signOut() {
    await logout();
    notify("You have signed out.", "info");
    navigate("/login");
  }

  return (
    <div className="app-frame">
      {mobileOpen && <button className="mobile-scrim" onClick={() => setMobileOpen(false)} aria-label="Close menu" />}
      <aside className={`sidebar ${mobileOpen ? "sidebar-open" : ""}`}>
        <div className="brand-lockup">
          <div className="brand-mark"><Layers3 size={21} strokeWidth={2.5} /></div>
          <div><div className="brand-name">learn<span>wise</span></div><div className="brand-caption">AI LEARNING PLATFORM</div></div>
          <button className="icon-btn mobile-close" onClick={() => setMobileOpen(false)} aria-label="Close menu"><X size={19} /></button>
        </div>
        <div className="sidebar-workspace">
          <div className="workspace-icon">L</div>
          <div className="workspace-copy"><b>Learning Institute</b><span>Workspace</span></div>
          <ChevronDown size={15} className="muted" />
        </div>
        <nav className="side-nav">
          {menu.map((group) => <div className="nav-group" key={group.section}>
            <div className="nav-section-label">{group.section}</div>
            {group.items.map(([label, to, Icon]) => <NavLink key={to} to={to} onClick={() => setMobileOpen(false)} className={({ isActive }) => `nav-link ${isActive ? "nav-active" : ""}`}>
              <Icon size={18} strokeWidth={1.8} /><span>{label}</span>{label === "AI Study Assistant" && <span className="nav-new">AI</span>}
            </NavLink>)}
          </div>)}
        </nav>
        <div className="sidebar-bottom">
          <div className="help-card"><div className="help-icon"><CircleHelp size={17} /></div><div><b>Need a hand?</b><span>Open AI study assistant</span></div><button className="icon-btn" onClick={() => navigate("/assistant")} aria-label="Open assistant"><ChevronDown size={15} className="rotate-neg" /></button></div>
          <div className="sidebar-user"><div className="avatar avatar-indigo">{user?.name?.charAt(0) || "U"}</div><div className="user-copy"><b>{user?.name}</b><span>{user?.role}</span></div><button className="icon-btn" onClick={signOut} title="Sign out" aria-label="Sign out"><LogOut size={17} /></button></div>
        </div>
      </aside>

      <main className="main-column">
        <header className="topbar">
          <button className="icon-btn mobile-menu" onClick={() => setMobileOpen(true)} aria-label="Open menu"><Menu size={20} /></button>
          <div className="breadcrumbs"><span>Workspace</span><span className="crumb-sep">/</span><b>{title}</b></div>
          <div className="topbar-right">
            <button className="top-search" onClick={() => setSearchOpen((v) => !v)}><Search size={16} /><span>Search courses</span><kbd>⌘ K</kbd></button>
            <button className="top-help" onClick={() => navigate("/assistant")} aria-label="Help"><CircleHelp size={18} /></button>
            <div className="profile-wrap">
              <button className="profile-trigger" onClick={() => setProfileOpen((v) => !v)}>
                <div className={`avatar ${user?.role === "Admin" ? "avatar-violet" : user?.role === "Trainer" ? "avatar-orange" : "avatar-indigo"}`}>{user?.name?.charAt(0) || "U"}</div>
                <div className="profile-name"><b>{user?.name}</b><span>{user?.role}</span></div><ChevronDown size={15} />
              </button>
              {profileOpen && <div className="profile-menu"><div className="profile-menu-head">Signed in as <b>{user?.email}</b></div><button onClick={signOut}><LogOut size={16} />Sign out</button></div>}
            </div>
          </div>
          {searchOpen && <div className="quick-search"><Search size={17} /><input autoFocus placeholder="Search courses…" onKeyDown={(e) => e.key === "Enter" && navigate(`/courses?search=${encodeURIComponent(e.currentTarget.value)}`)} /><button className="btn btn-small btn-primary" onClick={(e) => { const input = e.currentTarget.parentElement.querySelector("input"); navigate(`/courses?search=${encodeURIComponent(input.value)}`); setSearchOpen(false); }}>Search</button><button className="icon-btn" onClick={() => setSearchOpen(false)}><X size={17} /></button></div>}
        </header>
        <div className="page-wrap"><div className="page-heading"><div><h1>{title}</h1><p>{subtitle}</p></div><div className="heading-context"><span className="live-dot" /> All systems operational</div></div>{children}</div>
        <footer className="main-footer"><span>© 2026 Learnwise LMS</span><span>Built for better learning <span className="footer-heart">✦</span></span></footer>
      </main>
    </div>
  );
}
