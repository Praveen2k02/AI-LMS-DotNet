import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Area, AreaChart, Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { ArrowRight, Award, BookOpen, CalendarClock, ChartNoAxesCombined, CircleCheck, Clock3, GraduationCap, Sparkles, Users } from "lucide-react";
import { api } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const currency = new Intl.NumberFormat("en-IN", { style: "currency", currency: "INR", maximumFractionDigits: 0 });

export default function DashboardPage() {
  const { user } = useAuth();
  const notify = useToast();
  const [courses, setCourses] = useState([]);
  const [progress, setProgress] = useState([]);
  const [students, setStudents] = useState([]);
  const [trainers, setTrainers] = useState([]);
  const [assignments, setAssignments] = useState([]);
  const [loading, setLoading] = useState(true);
  const isStudent = user?.role === "Student";
  const isAdmin = user?.role === "Admin";

  useEffect(() => {
    let live = true;
    async function load() {
      setLoading(true);
      try {
        const courseRequest = api.get("/courses");
        if (isStudent) {
          const [c, p] = await Promise.all([courseRequest, api.get("/progress")]);
          if (!live) return;
          setCourses(c.data);
          setProgress(p.data);
        } else if (isAdmin) {
          const [c, s, t] = await Promise.all([courseRequest, api.get("/users?role=Student"), api.get("/users?role=Trainer")]);
          if (!live) return;
          setCourses(c.data);
          setStudents(s.data);
          setTrainers(t.data);
          const a = await api.get("/assignments");
          if (live) setAssignments(a.data);
        } else {
          const [c, s, a] = await Promise.all([courseRequest, api.get("/students"), api.get("/assignments")]);
          if (!live) return;
          const mine = c.data.filter((item) => item.trainerId === user.id);
          setCourses(mine);
          setStudents(s.data);
          setAssignments(a.data);
        }
      } catch (error) {
        notify(error.userMessage || "Could not load dashboard data.", "error");
      } finally { if (live) setLoading(false); }
    }
    load();
    return () => { live = false; };
  }, [isAdmin, isStudent, user?.id, notify]);

  const overall = progress.length ? Math.round(progress.reduce((sum, item) => sum + item.completionPercent, 0) / progress.length) : 0;
  const pendingAssignments = assignments.filter((a) => !a.isSubmitted && (!a.dueAtUtc || new Date(a.dueAtUtc) > new Date()));
  const chartCourses = useMemo(() => courses.slice(0, 7).map((c) => ({ name: c.title.length > 17 ? `${c.title.slice(0, 16)}…` : c.title, students: c.studentCount || 0 })), [courses]);
  const progressChart = progress.map((p) => ({ name: p.courseTitle.length > 15 ? `${p.courseTitle.slice(0, 14)}…` : p.courseTitle, completion: p.completionPercent, quiz: p.quizAveragePercent }));

  const greeting = new Date().getHours() < 12 ? "Good morning" : new Date().getHours() < 18 ? "Good afternoon" : "Good evening";

  return (
    <div className="page-stack">
      <section className="welcome-banner">
        <div className="welcome-copy"><div className="eyebrow eyebrow-light"><Sparkles size={14} /> YOUR LEARNING SPACE</div><h2>{greeting}, {user?.name?.split(" ")[0]} <span className="wave">✦</span></h2><p>{isStudent ? "A little progress each day adds up to big results. Pick up where you left off." : isAdmin ? "Here’s the latest view across your learning institute." : "Your classrooms are ready. See how your learners are progressing."}</p><div className="welcome-actions"><Link to="/courses" className="btn btn-white">{isStudent ? "Explore courses" : "Open courses"}<ArrowRight size={16} /></Link>{isStudent && <Link to="/assistant" className="btn btn-banner-outline"><Sparkles size={16} /> Ask AI tutor</Link>}</div></div>
        <div className="welcome-illustration"><div className="welcome-orb orb-a" /><div className="welcome-orb orb-b" /><div className="welcome-book"><BookOpen size={46} strokeWidth={1.3} /></div><div className="float-tag tag-one"><CircleCheck size={15} /> Keep going!</div><div className="float-tag tag-two"><Sparkles size={14} /> AI tutor ready</div></div>
      </section>

      {loading ? <div className="loading-card"><span className="spinner spinner-dark" /> Loading dashboard data from AI-LMS API…</div> : <>
        <div className="stat-grid">
          {isStudent ? <>
            <StatCard icon={BookOpen} label="Enrolled courses" value={progress.length} note="Your active learning" color="indigo" />
            <StatCard icon={ChartNoAxesCombined} label="Overall progress" value={`${overall}%`} note="Across all your courses" color="blue" />
            <StatCard icon={CircleCheck} label="Lessons completed" value={progress.reduce((s, p) => s + p.completedLessons, 0)} note={`${progress.reduce((s, p) => s + p.remainingLessons, 0)} lessons remaining`} color="green" />
            <StatCard icon={Award} label="Certificates" value={progress.filter((p) => p.certificateCode).length} note="Credentials earned" color="gold" />
          </> : isAdmin ? <>
            <StatCard icon={Users} label="Total students" value={students.length} note="Registered learners" color="indigo" />
            <StatCard icon={GraduationCap} label="Trainers" value={trainers.length} note="Teaching team" color="blue" />
            <StatCard icon={BookOpen} label="Courses" value={courses.length} note="Programs in your catalog" color="green" />
            <StatCard icon={ClipboardIcon} label="Pending work" value={pendingAssignments.length} note="Awaiting student submission" color="gold" />
          </> : <>
            <StatCard icon={BookOpen} label="My courses" value={courses.length} note="Assigned to you" color="indigo" />
            <StatCard icon={Users} label="Students" value={new Set(students.map((s) => s.id)).size} note="In your courses" color="blue" />
            <StatCard icon={Clock3} label="Pending work" value={pendingAssignments.length} note="Awaiting submission" color="gold" />
            <StatCard icon={ChartNoAxesCombined} label="Assessments" value={courses.reduce((s, c) => s + (c.lessonCount || 0), 0)} note="Lessons published" color="green" />
          </>}
        </div>

        <div className="dashboard-grid">
          <section className="panel chart-panel">
            <div className="panel-heading"><div><h3>{isStudent ? "Your learning progress" : "Course enrolments"}</h3><p>{isStudent ? "Completion across your enrolled courses" : "Active student enrolments by course"}</p></div><span className="panel-icon"><ChartNoAxesCombined size={18} /></span></div>
            <div className="chart-wrap">
              {isStudent ? progressChart.length ? <ResponsiveContainer width="100%" height="100%"><AreaChart data={progressChart} margin={{ top: 14, right: 18, left: -20, bottom: 0 }}><defs><linearGradient id="progressFill" x1="0" y1="0" x2="0" y2="1"><stop offset="5%" stopColor="#625cf5" stopOpacity={0.22} /><stop offset="95%" stopColor="#625cf5" stopOpacity={0.01} /></linearGradient></defs><CartesianGrid stroke="#edf0f6" strokeDasharray="4 4" vertical={false} /><XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fill: "#8690a5", fontSize: 11 }} /><YAxis domain={[0, 100]} axisLine={false} tickLine={false} tick={{ fill: "#8690a5", fontSize: 11 }} tickFormatter={(v) => `${v}%`} /><Tooltip formatter={(v) => [`${v}%`, "Completion"]} contentStyle={{ borderRadius: 12, border: "1px solid #e9ebf2", boxShadow: "0 8px 24px #24204712" }} /><Area type="monotone" dataKey="completion" stroke="#625cf5" strokeWidth={3} fill="url(#progressFill)" activeDot={{ r: 5 }} /></AreaChart></ResponsiveContainer> : <EmptyChart text="Enroll in a course to see your progress." /> : chartCourses.length ? <ResponsiveContainer width="100%" height="100%"><BarChart data={chartCourses} margin={{ top: 14, right: 16, left: -20, bottom: 0 }}><CartesianGrid stroke="#edf0f6" strokeDasharray="4 4" vertical={false} /><XAxis dataKey="name" axisLine={false} tickLine={false} tick={{ fill: "#8690a5", fontSize: 11 }} /><YAxis allowDecimals={false} axisLine={false} tickLine={false} tick={{ fill: "#8690a5", fontSize: 11 }} /><Tooltip formatter={(v) => [v, "Students"]} contentStyle={{ borderRadius: 12, border: "1px solid #e9ebf2" }} /><Bar dataKey="students" fill="#625cf5" radius={[7, 7, 2, 2]} maxBarSize={45} /></BarChart></ResponsiveContainer> : <EmptyChart text="Courses will appear here once created." />}
            </div>
          </section>

          <section className="panel progress-list-panel">
            <div className="panel-heading"><div><h3>{isStudent ? "Continue learning" : "Your courses"}</h3><p>{isStudent ? "Pick up where you left off" : "Quick access to your course workspace"}</p></div><Link to="/courses" className="panel-link">View all <ArrowRight size={14} /></Link></div>
            <div className="mini-course-list">
              {(isStudent ? progress : courses).slice(0, 4).map((item, i) => {
                const course = isStudent ? courses.find((c) => c.id === item.courseId) : item;
                return <Link to={`/courses/${item.courseId ?? item.id}`} className="mini-course" key={item.courseId ?? item.id}><div className={`course-swatch swatch-${i % 4}`}><BookOpen size={18} /></div><div className="mini-course-main"><b>{item.courseTitle ?? course?.title}</b><div className="mini-progress"><span><i style={{ width: `${item.completionPercent ?? 0}%` }} /></span><small>{item.completionPercent ?? 0}%</small></div></div><ArrowRight size={15} className="mini-arrow" /></Link>;
              })}
              {(isStudent ? progress : courses).length === 0 && <EmptyState icon={BookOpen} title="Nothing here yet" text={isStudent ? "Browse the catalog and enroll in your first course." : "Create or get assigned to a course to see it here."} action={<Link to="/courses" className="btn btn-primary btn-small">View courses</Link>} />}
            </div>
          </section>
        </div>

        {isStudent && <section className="panel table-panel"><div className="panel-heading"><div><h3>Course performance</h3><p>Scores, attendance and completion at a glance</p></div><Link to="/marks" className="panel-link">View marks <ArrowRight size={14} /></Link></div><div className="table-scroll"><table className="data-table"><thead><tr><th>Course</th><th>Lessons</th><th>Assignments</th><th>Quiz average</th><th>Attendance</th><th>Status</th></tr></thead><tbody>{progress.map((p) => <tr key={p.courseId}><td><Link className="table-course-link" to={`/courses/${p.courseId}`}>{p.courseTitle}</Link><small className="table-sub">{p.trainerName || "Learning program"}</small></td><td>{p.completedLessons}/{p.totalLessons}</td><td>{p.submittedAssignments}/{p.totalAssignments}</td><td><span className="score-pill">{p.quizAveragePercent}%</span></td><td>{p.attendancePercent}%</td><td><span className={`status-pill ${p.certificateEligible ? "status-done" : "status-progress"}`}>{p.certificateEligible ? "Certificate ready" : "In progress"}</span></td></tr>)}{progress.length === 0 && <tr><td colSpan="6" className="table-empty">Enroll in a course to see your learning report.</td></tr>}</tbody></table></div></section>}

        {isAdmin && <section className="panel table-panel"><div className="panel-heading"><div><h3>People at your institute</h3><p>Quick links into student and trainer management</p></div><Link to="/admin/users" className="panel-link">Manage users <ArrowRight size={14} /></Link></div><div className="people-preview-grid"><PeoplePreview title="Students" users={students.slice(0, 5)} icon={GraduationCap} color="blue" /><PeoplePreview title="Trainers" users={trainers.slice(0, 5)} icon={Sparkles} color="purple" /></div></section>}

        {!isStudent && <section className="panel table-panel"><div className="panel-heading"><div><h3>Assessment activity</h3><p>Submissions linked to your courses</p></div><Link to="/marks" className="panel-link">Open marks ledger <ArrowRight size={14} /></Link></div><div className="activity-summary"><div className="activity-number"><b>{assignments.length}</b><span>Assignments in catalog</span></div><div className="activity-number"><b>{assignments.filter((a) => a.isSubmitted).length}</b><span>Submissions recorded</span></div><div className="activity-number"><b>{pendingAssignments.length}</b><span>Not yet submitted</span></div><Link to="/courses" className="btn btn-primary btn-small">Open course tools <ArrowRight size={15} /></Link></div></section>}
      </>}
    </div>
  );
}

function ClipboardIcon(props) { return <CalendarClock {...props} />; }
function StatCard({ icon: Icon, label, value, note, color }) { return <div className="stat-card"><div className={`stat-icon stat-${color}`}><Icon size={20} /></div><div className="stat-label">{label}</div><div className="stat-value">{value}</div><div className="stat-note">{note}</div></div>; }
function EmptyChart({ text }) { return <div className="empty-chart"><div className="empty-chart-icon"><ChartNoAxesCombined size={22} /></div><span>{text}</span></div>; }
function EmptyState({ icon: Icon, title, text, action }) { return <div className="empty-state"><div className="empty-state-icon"><Icon size={21} /></div><b>{title}</b><p>{text}</p>{action}</div>; }
function PeoplePreview({ title, users, icon: Icon, color }) { return <div className="people-preview"><div className="people-preview-head"><span className={`people-preview-icon people-${color}`}><Icon size={17} /></span><b>{title}</b><span className="people-count">{users.length}</span></div>{users.length ? users.map((u) => <div className="people-row" key={u.id}><span className="avatar avatar-small avatar-indigo">{u.name.charAt(0)}</span><span><b>{u.name}</b><small>{u.email}</small></span></div>) : <p className="muted-small">No {title.toLowerCase()} found.</p>}</div>; }
