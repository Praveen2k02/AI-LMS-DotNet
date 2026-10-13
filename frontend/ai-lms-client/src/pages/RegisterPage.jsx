import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowLeft, ArrowRight, Eye, EyeOff, GraduationCap, Mail, Sparkles, UserRound } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { errorMessage } from "../api/client";

export default function RegisterPage() {
  const { register } = useAuth();
  const notify = useToast();
  const navigate = useNavigate();
  const [form, setForm] = useState({ name: "", email: "", password: "", role: "Student", phone: "", department: "", expertise: "" });
  const [show, setShow] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const update = (key, value) => setForm((f) => ({ ...f, [key]: value }));

  async function submit(e) {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      const user = await register(form);
      notify(`Welcome to Learnwise, ${user.name}!`, "success");
      navigate("/dashboard", { replace: true });
    } catch (err) { setError(errorMessage(err)); }
    finally { setBusy(false); }
  }

  return (
    <div className="auth-page">
      <div className="auth-ambient ambient-one" /><div className="auth-ambient ambient-two" />
      <header className="auth-top"><Link to="/login" className="auth-brand"><span className="brand-mark"><GraduationCap size={21} /></span><span>learn<span className="brand-highlight">wise</span></span></Link><Link to="/login" className="back-login"><ArrowLeft size={15} /> Back to sign in</Link></header>
      <main className="register-layout">
        <section className="auth-card register-card">
          <div className="auth-card-head"><div className="auth-icon"><Sparkles size={20} /></div><div><h2>Create your account</h2><p>Join the community and start learning.</p></div></div>
          <form onSubmit={submit} className="auth-form register-form">
            <label className="field-label">Full name</label><div className="input-wrap"><UserRound size={17} /><input required maxLength={100} placeholder="Your name" value={form.name} onChange={(e) => update("name", e.target.value)} /></div>
            <label className="field-label">Email address</label><div className="input-wrap"><Mail size={17} /><input required type="email" maxLength={254} placeholder="you@example.com" value={form.email} onChange={(e) => update("email", e.target.value)} /></div>
            <label className="field-label">Password <span className="label-hint">8 characters minimum</span></label><div className="input-wrap"><input required minLength={8} type={show ? "text" : "password"} placeholder="Create a secure password" value={form.password} onChange={(e) => update("password", e.target.value)} /><button type="button" className="password-toggle" onClick={() => setShow((v) => !v)} aria-label="Toggle password visibility">{show ? <EyeOff size={16} /> : <Eye size={16} />}</button></div>
            <label className="field-label">I am joining as</label><div className="role-choice">
              {["Student", "Trainer"].map((role) => <button type="button" key={role} onClick={() => update("role", role)} className={`role-option ${form.role === role ? "role-option-active" : ""}`}><span className={`role-option-icon ${role === "Trainer" ? "icon-orange" : "icon-blue"}`}>{role === "Trainer" ? <Sparkles size={17} /> : <GraduationCap size={17} />}</span><span><b>{role}</b><small>{role === "Trainer" ? "I teach courses" : "I want to learn"}</small></span><span className="radio-dot" /></button>)}
            </div>
            {form.role === "Trainer" ? <><label className="field-label">Expertise</label><div className="input-wrap"><input required maxLength={150} placeholder="e.g. React, SQL Server" value={form.expertise} onChange={(e) => update("expertise", e.target.value)} /></div></> : <><label className="field-label">Department <span className="label-hint">Optional</span></label><div className="input-wrap"><input maxLength={120} placeholder="e.g. Computer Science" value={form.department} onChange={(e) => update("department", e.target.value)} /></div></>}
            {error && <div className="form-error" role="alert">{error}</div>}
            <button type="submit" disabled={busy} className="btn btn-primary btn-block">{busy ? <span className="button-loading"><span className="spinner" /> Creating account…</span> : <>Create account <ArrowRight size={17} /></>}</button>
          </form>
          <p className="auth-register">Already have an account? <Link to="/login">Sign in</Link></p>
        </section>
      </main>
      <footer className="auth-footer"><span>© 2026 Learnwise LMS</span><span>By creating an account, you agree to the learning community guidelines.</span></footer>
    </div>
  );
}
