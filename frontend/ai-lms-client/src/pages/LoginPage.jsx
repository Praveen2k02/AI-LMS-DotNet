import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { ArrowRight, Eye, EyeOff, GraduationCap, LockKeyhole, Mail, Sparkles } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { errorMessage } from "../api/client";

const demos = [
  { role: "Admin", email: "admin@ailms.com", password: "admin123", text: "Manage the institute", initials: "A", tone: "demo-violet" },
  { role: "Trainer", email: "trainer@ailms.com", password: "trainer123", text: "Teach and inspire", initials: "T", tone: "demo-orange" },
  { role: "Student", email: "student@ailms.com", password: "student123", text: "Learn something new", initials: "S", tone: "demo-blue" },
];

export default function LoginPage() {
  const { login } = useAuth();
  const notify = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState("student@ailms.com");
  const [password, setPassword] = useState("student123");
  const [remember, setRemember] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  async function submit(e) {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      const user = await login(email, password, remember);
      notify(`Welcome back, ${user.name}!`, "success");
      navigate(location.state?.from || "/dashboard", { replace: true });
    } catch (err) {
      setError(errorMessage(err));
    } finally { setBusy(false); }
  }

  async function demo(demo) {
    setEmail(demo.email);
    setPassword(demo.password);
    setError("");
    setBusy(true);
    try {
      const user = await login(demo.email, demo.password, true);
      notify(`Signed in as ${user.role}`, "success");
      navigate("/dashboard", { replace: true });
    } catch (err) { setError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  return (
    <div className="auth-page">
      <div className="auth-ambient ambient-one" /><div className="auth-ambient ambient-two" />
      <header className="auth-top"><Link to="/login" className="auth-brand"><span className="brand-mark"><GraduationCap size={21} /></span><span>learn<span className="brand-highlight">wise</span></span></Link><span className="auth-top-note"><span className="live-dot" /> Your learning workspace</span></header>
      <main className="auth-layout">
        <section className="auth-story">
          <div className="eyebrow"><Sparkles size={15} /> INTELLIGENT LEARNING, MADE PERSONAL</div>
          <h1>Make your next<br />chapter <span>count.</span></h1>
          <p className="auth-story-text">A learning platform that brings your courses, mentors and AI-powered study tools together in one place.</p>
          <div className="story-stats"><div><b>Learn</b><span>Structured courses</span></div><div><b>Practise</b><span>Quizzes & projects</span></div><div><b>Achieve</b><span>Verified certificates</span></div></div>
          <div className="story-quote"><div className="quote-stars">★★★★★</div><p>“The best way to predict your future is to create it.”</p><span>— A good reminder to keep learning</span></div>
        </section>
        <section className="auth-card">
          <div className="auth-card-head"><div className="auth-icon"><LockKeyhole size={20} /></div><div><h2>Welcome back</h2><p>Sign in to continue your learning journey.</p></div></div>
          <form onSubmit={submit} className="auth-form">
            <label className="field-label" htmlFor="login-email">Email address</label>
            <div className="input-wrap"><Mail size={17} /><input id="login-email" type="email" autoComplete="email" placeholder="you@example.com" value={email} onChange={(e) => setEmail(e.target.value)} required /></div>
            <div className="field-between"><label className="field-label" htmlFor="login-password">Password</label><button type="button" className="text-link" onClick={() => notify("Contact your institute administrator to reset your password.", "info")}>Forgot password?</button></div>
            <div className="input-wrap"><LockKeyhole size={17} /><input id="login-password" type={showPassword ? "text" : "password"} autoComplete="current-password" placeholder="Enter your password" value={password} onChange={(e) => setPassword(e.target.value)} required /><button type="button" className="password-toggle" onClick={() => setShowPassword((v) => !v)} aria-label={showPassword ? "Hide password" : "Show password"}>{showPassword ? <EyeOff size={16} /> : <Eye size={16} />}</button></div>
            <label className="remember-row"><input type="checkbox" checked={remember} onChange={(e) => setRemember(e.target.checked)} /><span>Keep me signed in on this device</span></label>
            {error && <div className="form-error" role="alert">{error}</div>}
            <button type="submit" className="btn btn-primary btn-block btn-login" disabled={busy}>{busy ? <span className="button-loading"><span className="spinner" /> Signing in…</span> : <>Sign in <ArrowRight size={17} /></>}</button>
          </form>
          <div className="auth-divider"><span />or sign in instantly<span /></div>
          <div className="demo-grid">
            {demos.map((d) => <button key={d.role} type="button" className="demo-card" onClick={() => demo(d)} disabled={busy}><span className={`demo-avatar ${d.tone}`}>{d.initials}</span><span className="demo-copy"><b>{d.role}</b><small>{d.text}</small></span><ArrowRight size={15} className="demo-arrow" /></button>)}
          </div>
          <p className="auth-register">New to Learnwise? <Link to="/register">Create your account</Link></p>
        </section>
      </main>
      <footer className="auth-footer"><span>© 2026 Learnwise LMS</span><span>Secure JWT authentication · Your data stays protected</span></footer>
    </div>
  );
}
