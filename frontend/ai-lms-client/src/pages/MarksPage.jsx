import { useCallback, useEffect, useMemo, useState } from "react";
import { useSearchParams, Link } from "react-router-dom";
import { ArrowRight, Award, BookOpen, ChartNoAxesCombined, ClipboardList, Filter, Plus, Trash2 } from "lucide-react";
import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const blank = { courseId: "", studentId: "", title: "", source: "Exam", score: 0, maximumScore: 100 };

export default function MarksPage() {
  const { user } = useAuth();
  const notify = useToast();
  const [params] = useSearchParams();
  const [marks, setMarks] = useState([]);
  const [courses, setCourses] = useState([]);
  const [students, setStudents] = useState([]);
  const [courseFilter, setCourseFilter] = useState("");
  const [studentFilter, setStudentFilter] = useState(params.get("studentUserId") || "");
  const [loading, setLoading] = useState(true);
  const [formOpen, setFormOpen] = useState(false);
  const [form, setForm] = useState(blank);
  const staff = ["Admin", "Trainer"].includes(user?.role);
  const admin = user?.role === "Admin";

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const query = new URLSearchParams();
      if (courseFilter) query.set("courseId", courseFilter);
      if (staff && studentFilter) query.set("studentUserId", studentFilter);
      const [m, c] = await Promise.all([api.get(`/marks?${query}`), api.get("/courses")]);
      setMarks(m.data);
      setCourses(c.data);
      if (staff) {
        const s = await api.get("/students");
        setStudents(s.data);
      }
    } catch (err) { notify(errorMessage(err), "error"); }
    finally { setLoading(false); }
  }, [courseFilter, studentFilter, staff, notify]);

  useEffect(() => { load(); }, [load]);

  const average = marks.length ? Math.round(marks.reduce((sum, m) => sum + m.percentage, 0) / marks.length) : 0;
  const passed = marks.filter((m) => m.percentage >= 60).length;

  async function record(e) {
    e.preventDefault();
    try {
      await api.post("/marks", { ...form, courseId: Number(form.courseId), studentId: Number(form.studentId), score: Number(form.score), maximumScore: Number(form.maximumScore) });
      notify("Mark recorded successfully.", "success");
      setForm(blank); setFormOpen(false); await load();
    } catch (err) { notify(errorMessage(err), "error"); }
  }

  async function remove(mark) {
    if (!window.confirm(`Delete mark “${mark.title}” for ${mark.studentName}?`)) return;
    try { await api.delete(`/marks/${mark.id}`); notify("Mark deleted.", "info"); await load(); }
    catch (err) { notify(errorMessage(err), "error"); }
  }

  const grouped = useMemo(() => marks.reduce((acc, item) => { (acc[item.source] ||= []).push(item); return acc; }, {}), [marks]);

  return (
    <div className="page-stack">
      <div className="marks-overview-grid"><div className="marks-overview-card marks-overview-primary"><div className="marks-overview-icon"><ChartNoAxesCombined size={19} /></div><span>Overall average</span><b>{average}<small>%</small></b><div className="marks-meter"><i style={{ width: `${average}%` }} /></div><small>Based on {marks.length} assessments</small></div><div className="marks-overview-card"><div className="marks-overview-icon marks-green"><Award size={19} /></div><span>Passed</span><b>{passed}</b><small>Assessments above 60%</small></div><div className="marks-overview-card"><div className="marks-overview-icon marks-violet"><ClipboardList size={19} /></div><span>Recorded marks</span><b>{marks.length}</b><small>Across course activities</small></div><div className="marks-overview-card"><div className="marks-overview-icon marks-blue"><BookOpen size={19} /></div><span>Courses represented</span><b>{new Set(marks.map((m) => m.courseId)).size}</b><small>With assessment records</small></div></div>
      <section className="panel marks-panel"><div className="panel-heading marks-panel-heading"><div><h3>Marks ledger</h3><p>Quiz results, graded assignments and manual assessments.</p></div><div className="marks-toolbar"><label className="filter-select"><Filter size={15} /><select value={courseFilter} onChange={(e) => setCourseFilter(e.target.value)}><option value="">All courses</option>{courses.map((c) => <option key={c.id} value={c.id}>{c.title}</option>)}</select></label>{staff && <label className="filter-select"><select value={studentFilter} onChange={(e) => setStudentFilter(e.target.value)}><option value="">All students</option>{students.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}</select></label>}{staff && <button className="btn btn-primary btn-small" onClick={() => setFormOpen(true)}><Plus size={15} /> Record mark</button>}</div></div>
        {formOpen && <form className="manual-mark-form" onSubmit={record}><div className="manual-mark-heading"><b>Record a manual assessment</b><button type="button" className="text-link" onClick={() => setFormOpen(false)}>Cancel</button></div><div className="form-grid"><Field label="Course"><select required value={form.courseId} onChange={(e) => setForm({ ...form, courseId: e.target.value })}><option value="">Choose course…</option>{courses.map((c) => <option key={c.id} value={c.id}>{c.title}</option>)}</select></Field><Field label="Student"><select required value={form.studentId} onChange={(e) => setForm({ ...form, studentId: e.target.value })}><option value="">Choose student…</option>{students.map((s) => <option key={s.id} value={s.studentId}>{s.name}</option>)}</select></Field><Field label="Assessment title"><input required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder="e.g. Final examination" /></Field><Field label="Type"><select value={form.source} onChange={(e) => setForm({ ...form, source: e.target.value })}><option>Exam</option><option>Manual</option><option>Practical</option><option>Project</option></select></Field><Field label="Score"><input type="number" min="0" max={form.maximumScore} value={form.score} onChange={(e) => setForm({ ...form, score: e.target.value })} /></Field><Field label="Maximum score"><input type="number" min="1" value={form.maximumScore} onChange={(e) => setForm({ ...form, maximumScore: e.target.value })} /></Field></div><div className="form-actions"><button className="btn btn-primary">Save mark <Plus size={14} /></button></div></form>}
        {loading ? <div className="loading-card"><span className="spinner spinner-dark" /> Loading marks…</div> : marks.length ? <div className="table-scroll"><table className="data-table marks-table"><thead><tr>{staff && <th>Student</th>}<th>Course</th><th>Assessment</th><th>Type</th><th>Score</th><th>Grade</th>{staff && <th />}</tr></thead><tbody>{marks.map((m) => <tr key={m.id}>{staff && <td><span className="student-cell"><span className="avatar avatar-small avatar-indigo">{m.studentName.charAt(0)}</span><b>{m.studentName}</b></span></td>}<td><Link className="table-course-link" to={`/courses/${m.courseId}`}>{m.courseTitle}</Link></td><td><b>{m.title}</b><small className="table-sub">{new Date(m.createdAtUtc).toLocaleDateString()}</small></td><td><span className={`source-badge source-${m.source.toLowerCase()}`}>{m.source}</span></td><td><b>{m.score}</b><span className="score-denom"> / {m.maximumScore}</span><div className="score-track"><i style={{ width: `${m.percentage}%` }} /></div><small className="score-percent">{m.percentage}%</small></td><td><span className={`grade-badge ${m.grade === "F" || m.grade === "D" ? "grade-low" : "grade-good"}`}>{m.grade}</span></td>{staff && <td><button className="person-delete" onClick={() => remove(m)} title="Delete mark"><Trash2 size={15} /></button></td>}</tr>)}</tbody></table></div> : <div className="empty-state marks-empty"><div className="empty-state-icon"><Award size={21} /></div><b>No marks recorded yet</b><p>Quiz results and graded assignments will appear here automatically.</p></div>}
        {Object.keys(grouped).length > 0 && <div className="marks-legend">{Object.entries(grouped).map(([source, items]) => <span key={source}><i className={`legend-dot legend-${source.toLowerCase()}`} />{source} <b>{items.length}</b></span>)}</div>}
      </section>
    </div>
  );
}

function Field({ label, children }) { return <label className="form-field"><span>{label}</span>{children}</label>; }
