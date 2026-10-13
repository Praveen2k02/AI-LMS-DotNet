import { useCallback, useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowRight, BookOpen, Clock3, GraduationCap, Plus, Search, SlidersHorizontal, Users, X } from "lucide-react";
import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const emptyForm = { title: "", description: "", category: "Web Development", level: "Beginner", durationWeeks: 6, fee: 0, trainerId: "", thumbnailUrl: "" };
const gradients = ["course-gradient-purple", "course-gradient-blue", "course-gradient-green", "course-gradient-orange", "course-gradient-pink"];

export default function CoursesPage() {
  const { user } = useAuth();
  const notify = useToast();
  const [searchParams, setSearchParams] = useSearchParams();
  const [courses, setCourses] = useState([]);
  const [trainers, setTrainers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState(searchParams.get("search") || "");
  const [category, setCategory] = useState("");
  const [level, setLevel] = useState("");
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const staff = ["Admin", "Trainer"].includes(user?.role);
  const admin = user?.role === "Admin";

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams();
      if (query.trim()) params.set("search", query.trim());
      if (category) params.set("category", category);
      if (level) params.set("level", level);
      const { data } = await api.get(`/courses?${params.toString()}`);
      setCourses(data);
    } catch (err) { notify(errorMessage(err), "error"); }
    finally { setLoading(false); }
  }, [query, category, level, notify]);

  useEffect(() => { const timer = setTimeout(load, 180); return () => clearTimeout(timer); }, [load]);
  useEffect(() => {
    if (admin) api.get("/trainers").then((r) => setTrainers(r.data)).catch(() => {});
  }, [admin]);

  const categories = useMemo(() => [...new Set(courses.map((c) => c.category).filter(Boolean))].sort(), [courses]);

  async function createCourse(e) {
    e.preventDefault();
    setSaving(true);
    try {
      const payload = { ...form, durationWeeks: Number(form.durationWeeks), fee: Number(form.fee), trainerId: form.trainerId ? Number(form.trainerId) : null };
      const { data } = await api.post("/courses", payload);
      notify(`“${data.title}” is live in your catalog.`, "success");
      setCreateOpen(false);
      setForm(emptyForm);
      await load();
    } catch (err) { notify(errorMessage(err), "error"); }
    finally { setSaving(false); }
  }

  async function enroll(course) {
    try {
      await api.post(`/courses/${course.id}/enroll`, {});
      notify(`You’re enrolled in ${course.title}.`, "success");
      await load();
    } catch (err) { notify(errorMessage(err), "error"); }
  }

  async function unenroll(course) {
    if (!window.confirm(`Leave “${course.title}”? Your progress stays saved.`)) return;
    try {
      await api.delete(`/courses/${course.id}/enroll`);
      notify("You left the course.", "info");
      await load();
    } catch (err) { notify(errorMessage(err), "error"); }
  }

  async function deleteCourse(course) {
    if (!window.confirm(`Delete “${course.title}” and all its course material?`)) return;
    try {
      await api.delete(`/courses/${course.id}`);
      notify("Course deleted.", "info");
      await load();
    } catch (err) { notify(errorMessage(err), "error"); }
  }

  return (
    <div className="page-stack">
      <div className="course-toolbar panel">
        <div className="course-search"><Search size={18} /><input placeholder="Search courses, topics or skills…" value={query} onChange={(e) => { setQuery(e.target.value); setSearchParams(e.target.value ? { search: e.target.value } : {}); }} /><kbd>/</kbd></div>
        <div className="filter-selects"><label><SlidersHorizontal size={16} /><select value={category} onChange={(e) => setCategory(e.target.value)}><option value="">All categories</option>{categories.map((c) => <option key={c}>{c}</option>)}</select></label><select value={level} onChange={(e) => setLevel(e.target.value)}><option value="">Any level</option>{["Beginner", "Intermediate", "Advanced"].map((v) => <option key={v}>{v}</option>)}</select>{staff && <button className="btn btn-primary" onClick={() => setCreateOpen(true)}><Plus size={17} /> New course</button>}</div>
      </div>

      <div className="section-title-row"><div><h2>Explore your catalog</h2><p>{courses.length} course{courses.length !== 1 ? "s" : ""} to move you forward</p></div><div className="view-chip"><BookOpen size={15} /> Course library</div></div>

      {createOpen && <div className="modal-backdrop" onMouseDown={(e) => e.target === e.currentTarget && setCreateOpen(false)}><form className="modal-card" onSubmit={createCourse}><div className="modal-heading"><div><span className="eyebrow">COURSE SETUP</span><h2>Create a course</h2><p>Add a new program to your learning catalog.</p></div><button type="button" className="icon-btn" onClick={() => setCreateOpen(false)}><X size={19} /></button></div><div className="form-grid"><label className="form-field form-span-2"><span>Course title</span><input required minLength={3} maxLength={160} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} placeholder="e.g. Full Stack Web Development" /></label><label className="form-field form-span-2"><span>Description</span><textarea required minLength={10} rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="What will learners be able to do?" /></label><label className="form-field"><span>Category</span><input required maxLength={80} value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })} /></label><label className="form-field"><span>Level</span><select value={form.level} onChange={(e) => setForm({ ...form, level: e.target.value })}><option>Beginner</option><option>Intermediate</option><option>Advanced</option></select></label><label className="form-field"><span>Duration (weeks)</span><input type="number" min="1" max="104" value={form.durationWeeks} onChange={(e) => setForm({ ...form, durationWeeks: e.target.value })} /></label><label className="form-field"><span>Fee (₹)</span><input type="number" min="0" value={form.fee} onChange={(e) => setForm({ ...form, fee: e.target.value })} /></label>{admin && <label className="form-field form-span-2"><span>Assign trainer</span><select value={form.trainerId} onChange={(e) => setForm({ ...form, trainerId: e.target.value })}><option value="">Unassigned</option>{trainers.map((t) => <option key={t.id} value={t.id}>{t.name} · {t.expertise || t.email}</option>)}</select></label>}</div><div className="modal-actions"><button type="button" className="btn btn-ghost" onClick={() => setCreateOpen(false)}>Cancel</button><button disabled={saving} className="btn btn-primary">{saving ? "Creating…" : "Create course"}<ArrowRight size={16} /></button></div></form></div>}

      {loading ? <div className="course-grid">{[1,2,3,4,5,6].map((x) => <div className="course-skeleton" key={x}><div /><span /><span /><span /></div>)}</div> : courses.length ? <div className="course-grid">
        {courses.map((course, index) => <article className="course-card" key={course.id}>
          <div className={`course-cover ${gradients[index % gradients.length]}`}>
            <div className="course-cover-grid" /><span className="course-category-badge">{course.category}</span><span className="course-cover-icon"><BookOpen size={27} /></span><span className="course-level-badge">{course.level}</span>
          </div>
          <div className="course-card-body"><div className="course-trainer"><span className={`avatar avatar-small ${index % 2 ? "avatar-orange" : "avatar-indigo"}`}>{course.trainerName?.charAt(0) || "L"}</span><span>{course.trainerName || "Learnwise faculty"}</span><span className="trainer-verified"><GraduationCap size={13} /></span></div><Link to={`/courses/${course.id}`} className="course-title-link"><h3>{course.title}</h3></Link><p className="course-description">{course.description}</p><div className="course-meta"><span><Clock3 size={15} /> {course.durationWeeks} weeks</span><span><Users size={15} /> {course.studentCount} learners</span></div>{course.isEnrolled && <div className="card-progress"><div><span>Your progress</span><b>{course.progressPercent ?? 0}%</b></div><span className="progress-track"><i style={{ width: `${course.progressPercent ?? 0}%` }} /></span></div>}<div className="course-card-footer"><span className="course-price">{course.fee ? currency(course.fee) : <b className="free-label">Included</b>}</span><div className="course-actions">{user?.role === "Student" && (course.isEnrolled ? <button className="btn btn-outline btn-small" onClick={() => unenroll(course)}>Enrolled <span className="checkmark">✓</span></button> : <button className="btn btn-primary btn-small" onClick={() => enroll(course)}>Enroll <ArrowRight size={15} /></button>)}<Link to={`/courses/${course.id}`} className="btn btn-ghost btn-small">View course <ArrowRight size={14} /></Link>{admin && <button className="btn btn-delete-small" onClick={() => deleteCourse(course)} title="Delete course"><X size={15} /></button>}</div></div></div>
        </article>)}
      </div> : <div className="panel empty-results"><div className="empty-state-icon"><Search size={22} /></div><h3>No courses found</h3><p>Try a different search or clear the selected filters.</p><button className="btn btn-outline" onClick={() => { setQuery(""); setCategory(""); setLevel(""); }}>Clear filters</button></div>}
    </div>
  );
}

function currency(value) { return new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 }).format(value); }
