import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowLeft, BadgeCheck, BookOpenCheck, Search, ShieldCheck, XCircle } from "lucide-react";
import { api, errorMessage } from "../api/client";

export default function VerifyPage() {
  const [params] = useSearchParams();
  const [code, setCode] = useState(params.get("code") || "");
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => { const initial = params.get("code"); if (initial) check(initial); }, []);

  async function check(value = code) {
    if (!value.trim()) return;
    setLoading(true); setError(""); setResult(null);
    try { const { data } = await api.get(`/certificates/verify/${encodeURIComponent(value.trim())}`); setResult(data); }
    catch (err) { setError(errorMessage(err)); }
    finally { setLoading(false); }
  }

  return <div className="verify-page"><header className="verify-top"><Link to="/login" className="auth-brand"><span className="brand-mark"><BookOpenCheck size={20} /></span><span>learn<span className="brand-highlight">wise</span></span></Link><Link to="/login" className="back-login"><ArrowLeft size={15} /> Back to LMS</Link></header><main className="verify-main"><div className="verify-icon"><ShieldCheck size={28} /></div><div className="eyebrow">CREDENTIAL VALIDATION</div><h1>Verify a certificate</h1><p className="verify-intro">Confirm that a Learnwise certificate is authentic. Enter its unique verification code below.</p><form className="verify-form" onSubmit={(e) => { e.preventDefault(); check(); }}><div className="input-wrap"><Search size={17} /><input value={code} onChange={(e) => setCode(e.target.value)} placeholder="e.g. A1B2C3D4E5F67890" aria-label="Verification code" /><button className="btn btn-primary btn-small" disabled={loading}>{loading ? "Checking…" : "Verify credential"}</button></div></form>{error && <div className="verify-result verify-invalid"><XCircle size={21} /><div><b>Certificate not verified</b><p>{error.includes("not found") ? "No certificate matched this code. Check the code and try again." : error}</p></div></div>}{result && <div className="verify-result verify-valid"><div className="verify-success-icon"><BadgeCheck size={24} /></div><div className="verify-success-copy"><span className="verified-kicker"><span className="live-dot" /> AUTHENTIC CREDENTIAL</span><h2>Certificate verified</h2><p>This certificate is a valid record in the Learnwise learning system.</p><div className="verify-data"><div><small>AWARDED TO</small><b>{result.studentName}</b></div><div><small>COURSE</small><b>{result.courseTitle}</b></div><div><small>FINAL SCORE</small><b>{Math.round(result.finalScore)}%</b></div><div><small>ISSUE DATE</small><b>{new Date(result.issueDateUtc).toLocaleDateString()}</b></div><div><small>CERTIFICATE NUMBER</small><b>{result.certificateNumber}</b></div><div><small>VERIFICATION CODE</small><b className="mono-code">{result.verificationCode}</b></div></div></div></div>}<div className="verify-footnote">Certificates are issued after a learner meets the course completion requirements.</div></main><footer className="auth-footer"><span>© 2026 Learnwise LMS</span><span>Verification is public · No account required</span></footer></div>;
}
