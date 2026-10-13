import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
    ArrowLeft,
    ArrowRight,
    BookOpen,
    BrainCircuit,
    CalendarDays,
    Check,
    CheckCircle2,
    ChevronDown,
    Clock3,
    FileText,
    GraduationCap,
    Plus,
    Sparkles,
    Users
} from "lucide-react";
import { api, errorMessage } from "../api/client";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

const tabs = ["Overview", "Lessons", "Assignments", "Quizzes", "Students", "AI Tools"];

const blankQuestion = () => ({
    text: "",
    options: ["", "", "", ""],
    correctOptionIndex: 0,
    marks: 1,
    explanation: ""
});

export default function CourseDetailPage() {
    const { id } = useParams();
    const { user } = useAuth();
    const notify = useToast();

    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [activeTab, setActiveTab] = useState("Overview");
    const [expandedLesson, setExpandedLesson] = useState(null);
    const [staff, setStaff] = useState({ trainers: [], students: [] });
    const [attendance, setAttendance] = useState([]);

    const isStaff = ["Admin", "Trainer"].includes(user?.role);
    const isAdmin = user?.role === "Admin";
    const isStudent = user?.role === "Student";

    // Students should never see the staff-only AI Tools tab.
    const visibleTabs = isStaff
        ? tabs
        : tabs.filter((tab) => tab !== "AI Tools");

    const load = useCallback(async () => {
        try {
            const { data: course } = await api.get(`/courses/${id}`);
            setData(course);
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setLoading(false);
        }
    }, [id, notify]);

    useEffect(() => {
        setLoading(true);
        load();
    }, [load]);

    useEffect(() => {
        if (!isStaff) return;

        Promise.all([
            api.get("/trainers"),
            api.get("/students")
        ])
            .then(([t, s]) =>
                setStaff({
                    trainers: t.data,
                    students: s.data
                })
            )
            .catch(() => { });
    }, [isStaff]);

    useEffect(() => {
        if (!isStaff || !id || activeTab !== "Students") return;

        api
            .get(
                `/attendance?courseId=${id}&date=${new Date()
                    .toISOString()
                    .slice(0, 10)}`
            )
            .then((r) => setAttendance(r.data))
            .catch((err) => notify(errorMessage(err), "error"));
    }, [id, activeTab, isStaff, notify]);

    // Safety: if a student somehow has AI Tools selected from an old URL/state,
    // return them to Overview.
    useEffect(() => {
        if (!isStaff && activeTab === "AI Tools") {
            setActiveTab("Overview");
        }
    }, [isStaff, activeTab]);

    if (loading) {
        return (
            <div className="loading-card">
                <span className="spinner spinner-dark" />
                Loading course workspace…
            </div>
        );
    }

    if (!data) {
        return (
            <div className="panel empty-results">
                <h3>Course not found</h3>
                <Link to="/courses" className="btn btn-primary">
                    Back to courses
                </Link>
            </div>
        );
    }

    const { course, lessons, assignments, quizzes, students } = data;

    const completed = lessons.filter((l) => l.isCompleted).length;

    const lessonProgress = lessons.length
        ? Math.round((completed / lessons.length) * 100)
        : 0;

    async function enroll() {
        try {
            await api.post(`/courses/${id}/enroll`, {});
            notify("You are enrolled in this course.", "success");
            await load();
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    async function unenroll() {
        if (
            !window.confirm(
                "Leave this course? Your saved progress stays in the system."
            )
        ) {
            return;
        }

        try {
            await api.delete(`/courses/${id}/enroll`);
            notify("You left the course.", "info");
            await load();
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    async function deleteCourse() {
        if (
            !window.confirm(
                `Delete “${course.title}” and all course content?`
            )
        ) {
            return;
        }

        try {
            await api.delete(`/courses/${id}`);
            notify("Course deleted.", "info");
            window.location.assign("/courses");
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    return (
        <div className="page-stack">
            <Link to="/courses" className="back-link">
                <ArrowLeft size={15} />
                Back to courses
            </Link>

            <section className="detail-hero">
                <div className="detail-hero-pattern" />

                <div className="detail-hero-content">
                    <div className="detail-badges">
                        <span className="hero-chip">{course.category}</span>

                        <span className="hero-chip hero-chip-light">
                            {course.level}
                        </span>

                        {course.isEnrolled && (
                            <span className="hero-chip hero-chip-success">
                                <CheckCircle2 size={13} />
                                Enrolled
                            </span>
                        )}
                    </div>

                    <h1>{course.title}</h1>

                    <p>{course.description}</p>

                    <div className="detail-meta">
                        <span>
                            <GraduationCap size={16} />
                            {course.trainerName || "Learnwise faculty"}
                        </span>

                        <span>
                            <Clock3 size={16} />
                            {course.durationWeeks} weeks
                        </span>

                        <span>
                            <Users size={16} />
                            {course.studentCount} learners
                        </span>

                        <span>
                            <BookOpen size={16} />
                            {lessons.length} lessons
                        </span>
                    </div>

                    <div className="detail-hero-actions">
                        {isStudent &&
                            (course.isEnrolled ? (
                                <button
                                    className="btn btn-white"
                                    onClick={unenroll}
                                >
                                    Leave course
                                    <ChevronDown size={15} />
                                </button>
                            ) : (
                                <button
                                    className="btn btn-white"
                                    onClick={enroll}
                                >
                                    Enroll now
                                    <ArrowRight size={16} />
                                </button>
                            ))}

                        {isStaff && (
                            <button
                                className="btn btn-banner-outline"
                                onClick={() => setActiveTab("AI Tools")}
                            >
                                <Sparkles size={16} />
                                Open AI tools
                            </button>
                        )}

                        {isAdmin && (
                            <button
                                className="btn btn-banner-outline btn-danger-outline"
                                onClick={deleteCourse}
                            >
                                Delete course
                            </button>
                        )}
                    </div>
                </div>

                <div className="detail-progress-card">
                    <div className="detail-progress-icon">
                        <ChartIcon />
                    </div>

                    <span>Your progress</span>

                    <b>
                        {course.progressPercent ??
                            (isStudent && course.isEnrolled
                                ? lessonProgress
                                : 0)}
                        <small>%</small>
                    </b>

                    <div className="hero-progress">
                        <i
                            style={{
                                width: `${course.progressPercent ??
                                    (isStudent && course.isEnrolled
                                        ? lessonProgress
                                        : 0)
                                    }%`
                            }}
                        />
                    </div>

                    <span className="detail-progress-caption">
                        {completed} of {lessons.length} lessons completed
                    </span>
                </div>
            </section>

            {!course.isEnrolled && isStudent && (
                <div className="callout callout-info">
                    <Sparkles size={17} />

                    <span>
                        Enroll in this course to save your progress, submit
                        assignments and take quizzes.
                    </span>

                    <button
                        className="btn btn-primary btn-small"
                        onClick={enroll}
                    >
                        Enroll free
                        <ArrowRight size={14} />
                    </button>
                </div>
            )}

            <div className="tabs-bar">
                {visibleTabs.map((tab) => (
                    <button
                        key={tab}
                        className={`tab-button ${activeTab === tab ? "tab-active" : ""
                            }`}
                        onClick={() => setActiveTab(tab)}
                    >
                        {tab}

                        {tab === "Lessons" && (
                            <span className="tab-count">
                                {lessons.length}
                            </span>
                        )}

                        {tab === "Assignments" && (
                            <span className="tab-count">
                                {assignments.length}
                            </span>
                        )}

                        {tab === "Quizzes" && (
                            <span className="tab-count">
                                {quizzes.length}
                            </span>
                        )}
                    </button>
                ))}
            </div>

            {activeTab === "Overview" && (
                <div className="detail-content-grid">
                    <div className="page-stack">
                        <section className="panel detail-panel">
                            <div className="panel-heading">
                                <div>
                                    <h3>About this course</h3>
                                    <p>What you’ll learn in this program</p>
                                </div>

                                <span className="panel-icon">
                                    <BookOpen size={18} />
                                </span>
                            </div>

                            <p className="detail-description">
                                {course.description}
                            </p>

                            <div className="learning-outcomes">
                                <b>Course at a glance</b>

                                <div className="outcomes-grid">
                                    <div>
                                        <span>{lessons.length}</span>
                                        <small>Lessons</small>
                                    </div>

                                    <div>
                                        <span>{assignments.length}</span>
                                        <small>Assignments</small>
                                    </div>

                                    <div>
                                        <span>{quizzes.length}</span>
                                        <small>Quizzes</small>
                                    </div>

                                    <div>
                                        <span>{course.durationWeeks}</span>
                                        <small>Weeks</small>
                                    </div>
                                </div>
                            </div>
                        </section>

                        <section className="panel detail-panel">
                            <div className="panel-heading">
                                <div>
                                    <h3>Course outline</h3>
                                    <p>
                                        Start with the first lesson and build your skills
                                    </p>
                                </div>

                                <button
                                    className="panel-link"
                                    onClick={() => setActiveTab("Lessons")}
                                >
                                    All lessons
                                    <ArrowRight size={14} />
                                </button>
                            </div>

                            <div className="outline-list">
                                {lessons.slice(0, 5).map((lesson, index) => (
                                    <button
                                        className="outline-row"
                                        key={lesson.id}
                                        onClick={() => {
                                            setActiveTab("Lessons");
                                            setExpandedLesson(lesson.id);
                                        }}
                                    >
                                        <span
                                            className={`outline-num ${lesson.isCompleted
                                                    ? "outline-done"
                                                    : ""
                                                }`}
                                        >
                                            {lesson.isCompleted ? (
                                                <Check size={14} />
                                            ) : (
                                                String(index + 1).padStart(2, "0")
                                            )}
                                        </span>

                                        <span className="outline-copy">
                                            <b>{lesson.title}</b>
                                            <small>
                                                {lesson.durationMinutes} min lesson
                                            </small>
                                        </span>

                                        <ArrowRight size={15} className="muted" />
                                    </button>
                                ))}

                                {lessons.length === 0 && (
                                    <p className="empty-inline">
                                        No lessons have been added yet.
                                    </p>
                                )}
                            </div>
                        </section>
                    </div>

                    <aside className="page-stack">
                        <section className="panel trainer-card">
                            <div className="trainer-card-top">
                                <div className="trainer-photo">
                                    {course.trainerName?.charAt(0) || "L"}
                                </div>

                                <span className="trainer-badge">
                                    <CheckCircle2 size={13} />
                                    Verified trainer
                                </span>
                            </div>

                            <span className="eyebrow">
                                YOUR INSTRUCTOR
                            </span>

                            <h3>
                                {course.trainerName || "Learnwise faculty"}
                            </h3>

                            <p className="trainer-bio">
                                Expert-led learning with practical examples and
                                supportive feedback throughout the course.
                            </p>

                            <div className="trainer-tags">
                                <span>Mentorship</span>
                                <span>Practical projects</span>
                            </div>
                        </section>

                        <section className="panel detail-summary">
                            <div className="summary-icon">
                                <Sparkles size={17} />
                            </div>

                            <b>Learn at your own pace</b>

                            <p>
                                Lessons, assessments and AI-powered study tools
                                are all in one place.
                            </p>

                            {isStaff && (
                                <button
                                    className="panel-link"
                                    onClick={() => setActiveTab("AI Tools")}
                                >
                                    Explore AI tools
                                    <ArrowRight size={14} />
                                </button>
                            )}
                        </section>
                    </aside>
                </div>
            )}

            {activeTab === "Lessons" && (
                <section className="page-stack">
                    {isStaff && (
                        <LessonCreate
                            courseId={id}
                            onDone={load}
                            notify={notify}
                        />
                    )}

                    {lessons.map((lesson, index) => (
                        <article
                            className="lesson-card panel"
                            key={lesson.id}
                        >
                            <button
                                className="lesson-card-main"
                                onClick={() =>
                                    setExpandedLesson(
                                        expandedLesson === lesson.id
                                            ? null
                                            : lesson.id
                                    )
                                }
                            >
                                <span
                                    className={`lesson-number ${lesson.isCompleted
                                            ? "lesson-number-done"
                                            : ""
                                        }`}
                                >
                                    {lesson.isCompleted ? (
                                        <Check size={16} />
                                    ) : (
                                        String(index + 1).padStart(2, "0")
                                    )}
                                </span>

                                <span className="lesson-title-block">
                                    <b>{lesson.title}</b>
                                    <small>
                                        {lesson.durationMinutes} minutes · Lesson{" "}
                                        {index + 1}
                                    </small>
                                </span>

                                <span className="lesson-status">
                                    {lesson.isCompleted ? (
                                        <span className="status-pill status-done">
                                            Completed
                                        </span>
                                    ) : (
                                        <span className="status-pill status-progress">
                                            Ready to learn
                                        </span>
                                    )}
                                </span>

                                <ChevronDown
                                    size={18}
                                    className={`lesson-chevron ${expandedLesson === lesson.id
                                            ? "chevron-open"
                                            : ""
                                        }`}
                                />
                            </button>

                            {expandedLesson === lesson.id && (
                                <div className="lesson-expanded">
                                    <div className="lesson-text">
                                        <h4>
                                            {lesson.description || lesson.title}
                                        </h4>

                                        <p>
                                            {lesson.content ||
                                                "Your trainer will add lesson material soon."}
                                        </p>

                                        {lesson.videoUrl && (
                                            <a
                                                href={lesson.videoUrl}
                                                target="_blank"
                                                rel="noreferrer"
                                                className="resource-link"
                                            >
                                                <ArrowRight size={14} />
                                                Open lesson video
                                            </a>
                                        )}

                                        {lesson.documentUrl && (
                                            <a
                                                href={lesson.documentUrl}
                                                target="_blank"
                                                rel="noreferrer"
                                                className="resource-link"
                                            >
                                                <FileText size={14} />
                                                Download lesson document
                                            </a>
                                        )}
                                    </div>

                                    <div className="lesson-actions">
                                        {isStudent &&
                                            course.isEnrolled && (
                                                <button
                                                    className={`btn ${lesson.isCompleted
                                                            ? "btn-outline"
                                                            : "btn-primary"
                                                        }`}
                                                    onClick={async () => {
                                                        try {
                                                            await api.post(
                                                                `/lessons/${lesson.id}/complete`
                                                            );

                                                            notify(
                                                                lesson.isCompleted
                                                                    ? "Lesson marked incomplete."
                                                                    : "Lesson completed!",
                                                                "success"
                                                            );

                                                            await load();
                                                        } catch (err) {
                                                            notify(
                                                                errorMessage(err),
                                                                "error"
                                                            );
                                                        }
                                                    }}
                                                >
                                                    {lesson.isCompleted
                                                        ? "Mark incomplete"
                                                        : "Mark complete"}

                                                    <Check size={15} />
                                                </button>
                                            )}

                                        {isStaff && (
                                            <button
                                                className="btn btn-ghost"
                                                onClick={() =>
                                                    setActiveTab("AI Tools")
                                                }
                                            >
                                                <Sparkles size={15} />
                                                Summarise with AI
                                            </button>
                                        )}

                                        {isStaff && (
                                            <button
                                                className="btn btn-delete-small"
                                                onClick={async () => {
                                                    if (
                                                        !window.confirm(
                                                            `Delete lesson “${lesson.title}”?`
                                                        )
                                                    ) {
                                                        return;
                                                    }

                                                    try {
                                                        await api.delete(
                                                            `/lessons/${lesson.id}`
                                                        );

                                                        notify(
                                                            "Lesson deleted.",
                                                            "info"
                                                        );

                                                        await load();
                                                    } catch (err) {
                                                        notify(
                                                            errorMessage(err),
                                                            "error"
                                                        );
                                                    }
                                                }}
                                            >
                                                Delete lesson
                                            </button>
                                        )}
                                    </div>
                                </div>
                            )}
                        </article>
                    ))}

                    {lessons.length === 0 && (
                        <EmptyCard
                            icon={BookOpen}
                            title="No lessons published"
                            text={
                                isStaff
                                    ? "Add the first lesson above to get this course started."
                                    : "Your trainer will publish course content soon."
                            }
                        />
                    )}
                </section>
            )}

            {activeTab === "Assignments" && (
                <section className="page-stack">
                    {isStaff && (
                        <AssignmentCreate
                            courseId={id}
                            onDone={load}
                            notify={notify}
                        />
                    )}

                    {assignments.map((assignment) => (
                        <AssignmentCard
                            key={assignment.id}
                            assignment={assignment}
                            isStaff={isStaff}
                            isStudent={isStudent}
                            courseEnrolled={course.isEnrolled}
                            onChanged={load}
                            notify={notify}
                        />
                    ))}

                    {assignments.length === 0 && (
                        <EmptyCard
                            icon={FileText}
                            title="No assignments yet"
                            text={
                                isStaff
                                    ? "Create an assignment for your learners."
                                    : "Assignments will appear here when your trainer publishes them."
                            }
                        />
                    )}
                </section>
            )}

            {activeTab === "Quizzes" && (
                <section className="page-stack">
                    {isStaff && (
                        <ManualQuizCreate
                            courseId={id}
                            onDone={load}
                            notify={notify}
                        />
                    )}

                    {quizzes.map((quiz) => (
                        <div
                            className="quiz-list-card panel"
                            key={quiz.id}
                        >
                            <div className="quiz-list-icon">
                                <BrainCircuit size={21} />
                            </div>

                            <div className="quiz-list-copy">
                                <b>{quiz.title}</b>

                                <p>
                                    {quiz.description ||
                                        `${quiz.questions?.length || 0} questions`}{" "}
                                    · {quiz.timeLimitMinutes} minutes{" "}
                                    {quiz.isAiGenerated && (
                                        <span className="ai-generated-pill">
                                            <Sparkles size={12} />
                                            AI generated
                                        </span>
                                    )}
                                </p>
                            </div>

                            <div className="quiz-list-actions">
                                {isStudent ? (
                                    <Link
                                        to={`/quizzes/${quiz.id}`}
                                        className="btn btn-primary btn-small"
                                    >
                                        Take quiz
                                        <ArrowRight size={14} />
                                    </Link>
                                ) : (
                                    <Link
                                        to={`/quizzes/${quiz.id}`}
                                        className="btn btn-outline btn-small"
                                    >
                                        Preview
                                        <ArrowRight size={14} />
                                    </Link>
                                )}

                                {isStaff && (
                                    <button
                                        className="btn btn-delete-small"
                                        onClick={async () => {
                                            if (
                                                !window.confirm(
                                                    `Delete “${quiz.title}”?`
                                                )
                                            ) {
                                                return;
                                            }

                                            try {
                                                await api.delete(
                                                    `/quizzes/${quiz.id}`
                                                );

                                                notify(
                                                    "Quiz deleted.",
                                                    "info"
                                                );

                                                await load();
                                            } catch (err) {
                                                notify(
                                                    errorMessage(err),
                                                    "error"
                                                );
                                            }
                                        }}
                                    >
                                        Delete
                                    </button>
                                )}
                            </div>
                        </div>
                    ))}

                    {quizzes.length === 0 && (
                        <EmptyCard
                            icon={BrainCircuit}
                            title="No quizzes available"
                            text={
                                isStaff
                                    ? "Create a quiz manually or generate one with AI."
                                    : "Your trainer will publish a quiz here."
                            }
                        />
                    )}
                </section>
            )}

            {activeTab === "Students" && (
                <section className="page-stack">
                    {isStaff &&
                        staff.students.length > 0 && (
                            <EnrollStudent
                                courseId={id}
                                students={staff.students.filter(
                                    (s) =>
                                        !students.some(
                                            (en) => en.id === s.id
                                        )
                                )}
                                onDone={load}
                                notify={notify}
                            />
                        )}

                    {students.length ? (
                        <div className="panel table-panel">
                            <div className="panel-heading">
                                <div>
                                    <h3>Course roster</h3>
                                    <p>
                                        {students.length} enrolled learners
                                    </p>
                                </div>
                            </div>

                            <div className="table-scroll">
                                <table className="data-table">
                                    <thead>
                                        <tr>
                                            <th>Student</th>
                                            <th>Email</th>

                                            {isStaff && (
                                                <th>Attendance today</th>
                                            )}

                                            <th>Actions</th>
                                        </tr>
                                    </thead>

                                    <tbody>
                                        {students.map((student) => {
                                            const row = attendance.find(
                                                (a) =>
                                                    a.studentId ===
                                                    student.studentId
                                            );

                                            return (
                                                <tr key={student.id}>
                                                    <td>
                                                        <span className="student-cell">
                                                            <span className="avatar avatar-small avatar-indigo">
                                                                {student.name.charAt(0)}
                                                            </span>

                                                            <b>{student.name}</b>
                                                        </span>
                                                    </td>

                                                    <td>{student.email}</td>

                                                    {isStaff && (
                                                        <td>
                                                            <button
                                                                className={`attendance-toggle ${row?.isPresent
                                                                        ? "attendance-present"
                                                                        : "attendance-absent"
                                                                    }`}
                                                                onClick={async () => {
                                                                    try {
                                                                        await api.post(
                                                                            "/attendance",
                                                                            {
                                                                                courseId:
                                                                                    Number(id),
                                                                                studentId:
                                                                                    student.studentId,
                                                                                date: new Date()
                                                                                    .toISOString()
                                                                                    .slice(0, 10),
                                                                                isPresent:
                                                                                    !row?.isPresent
                                                                            }
                                                                        );

                                                                        notify(
                                                                            `${student.name} marked ${row?.isPresent
                                                                                ? "absent"
                                                                                : "present"
                                                                            }.`,
                                                                            "success"
                                                                        );

                                                                        const r =
                                                                            await api.get(
                                                                                `/attendance?courseId=${id}&date=${new Date()
                                                                                    .toISOString()
                                                                                    .slice(0, 10)}`
                                                                            );

                                                                        setAttendance(
                                                                            r.data
                                                                        );
                                                                    } catch (err) {
                                                                        notify(
                                                                            errorMessage(err),
                                                                            "error"
                                                                        );
                                                                    }
                                                                }}
                                                            >
                                                                {row?.isPresent
                                                                    ? "✓ Present"
                                                                    : "Mark present"}
                                                            </button>
                                                        </td>
                                                    )}

                                                    <td>
                                                        {isStaff ? (
                                                            <Link
                                                                to={`/marks?studentUserId=${student.id}`}
                                                                className="table-link"
                                                            >
                                                                View marks
                                                                <ArrowRight size={13} />
                                                            </Link>
                                                        ) : (
                                                            "—"
                                                        )}
                                                    </td>
                                                </tr>
                                            );
                                        })}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    ) : (
                        <EmptyCard
                            icon={Users}
                            title="No students enrolled"
                            text="Learners will appear here once they join the course."
                        />
                    )}
                </section>
            )}

            {activeTab === "AI Tools" && isStaff && (
                <AITools
                    courseId={Number(id)}
                    lessons={lessons}
                    onDone={load}
                    notify={notify}
                />
            )}
        </div>
    );
}

function LessonCreate({ courseId, onDone, notify }) {
    const [open, setOpen] = useState(false);
    const [form, setForm] = useState({
        title: "",
        description: "",
        content: "",
        durationMinutes: 30
    });

    if (!open) {
        return (
            <button
                className="add-inline-card"
                onClick={() => setOpen(true)}
            >
                <span className="add-inline-icon">
                    <Plus size={18} />
                </span>

                <span>
                    <b>Add a lesson</b>
                    <small>
                        Create the next lesson in your course
                    </small>
                </span>

                <ArrowRight size={17} />
            </button>
        );
    }

    return (
        <form
            className="panel create-inline-form"
            onSubmit={async (e) => {
                e.preventDefault();

                try {
                    await api.post(`/courses/${courseId}/lessons`, {
                        ...form,
                        durationMinutes: Number(
                            form.durationMinutes
                        )
                    });

                    notify(
                        "Lesson added successfully.",
                        "success"
                    );

                    setForm({
                        title: "",
                        description: "",
                        content: "",
                        durationMinutes: 30
                    });

                    setOpen(false);
                    await onDone();
                } catch (err) {
                    notify(errorMessage(err), "error");
                }
            }}
        >
            <div className="panel-heading">
                <div>
                    <h3>New lesson</h3>
                    <p>Lesson content is stored in SQL Server.</p>
                </div>

                <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    onClick={() => setOpen(false)}
                >
                    Cancel
                </button>
            </div>

            <div className="form-grid">
                <Field label="Lesson title">
                    <input
                        required
                        value={form.title}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                title: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Duration (minutes)">
                    <input
                        type="number"
                        min="1"
                        value={form.durationMinutes}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                durationMinutes: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Description" span>
                    <input
                        value={form.description}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                description: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Lesson content" span>
                    <textarea
                        rows="4"
                        value={form.content}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                content: e.target.value
                            })
                        }
                    />
                </Field>
            </div>

            <button className="btn btn-primary">
                Publish lesson
                <ArrowRight size={15} />
            </button>
        </form>
    );
}

function AssignmentCreate({
    courseId,
    onDone,
    notify
}) {
    const [open, setOpen] = useState(false);

    const [form, setForm] = useState({
        title: "",
        description: "",
        maximumMarks: 100,
        dueAtUtc: ""
    });

    if (!open) {
        return (
            <button
                className="add-inline-card"
                onClick={() => setOpen(true)}
            >
                <span className="add-inline-icon">
                    <Plus size={18} />
                </span>

                <span>
                    <b>Create assignment</b>
                    <small>
                        Set instructions, due date and marks
                    </small>
                </span>

                <ArrowRight size={17} />
            </button>
        );
    }

    return (
        <form
            className="panel create-inline-form"
            onSubmit={async (e) => {
                e.preventDefault();

                try {
                    await api.post(
                        `/courses/${courseId}/assignments`,
                        {
                            ...form,
                            maximumMarks: Number(
                                form.maximumMarks
                            ),
                            dueAtUtc: form.dueAtUtc
                                ? new Date(
                                    form.dueAtUtc
                                ).toISOString()
                                : null
                        }
                    );

                    notify(
                        "Assignment published.",
                        "success"
                    );

                    setOpen(false);
                    await onDone();
                } catch (err) {
                    notify(errorMessage(err), "error");
                }
            }}
        >
            <div className="panel-heading">
                <div>
                    <h3>New assignment</h3>
                    <p>
                        Learners can submit before the deadline.
                    </p>
                </div>

                <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    onClick={() => setOpen(false)}
                >
                    Cancel
                </button>
            </div>

            <div className="form-grid">
                <Field label="Title">
                    <input
                        required
                        value={form.title}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                title: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Maximum marks">
                    <input
                        type="number"
                        min="1"
                        value={form.maximumMarks}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                maximumMarks: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Due date" span>
                    <input
                        type="datetime-local"
                        value={form.dueAtUtc}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                dueAtUtc: e.target.value
                            })
                        }
                    />
                </Field>

                <Field label="Instructions" span>
                    <textarea
                        required
                        rows="3"
                        value={form.description}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                description: e.target.value
                            })
                        }
                    />
                </Field>
            </div>

            <button className="btn btn-primary">
                Publish assignment
                <ArrowRight size={15} />
            </button>
        </form>
    );
}

function AssignmentCard({
    assignment: a,
    isStaff,
    isStudent,
    courseEnrolled,
    onChanged,
    notify
}) {
    const [text, setText] = useState("");
    const [subs, setSubs] = useState(null);
    const [marks, setMarks] = useState(0);
    const [feedback, setFeedback] = useState("");
    const [busy, setBusy] = useState(false);

    async function submit() {
        setBusy(true);

        try {
            await api.post(
                `/assignments/${a.id}/submissions`,
                { text }
            );

            notify(
                "Assignment submitted.",
                "success"
            );

            setText("");
            await onChanged();
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setBusy(false);
        }
    }

    async function loadSubs() {
        try {
            const r = await api.get(
                `/assignments/${a.id}/submissions`
            );

            setSubs(r.data);
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    return (
        <article className="panel assignment-card">
            <div className="assignment-card-head">
                <div className="assignment-symbol">
                    <FileText size={19} />
                </div>

                <div className="assignment-main">
                    <div className="assignment-title-row">
                        <h3>{a.title}</h3>
                        <span className="marks-max">
                            {a.maximumMarks} marks
                        </span>
                    </div>

                    <p>{a.description}</p>

                    <div className="assignment-info">
                        <span>
                            <CalendarDays size={14} />

                            {a.dueAtUtc
                                ? `Due ${new Date(
                                    a.dueAtUtc
                                ).toLocaleString()}`
                                : "No deadline"}
                        </span>

                        {a.isSubmitted && (
                            <span
                                className={`status-pill ${a.marks != null
                                        ? "status-done"
                                        : "status-progress"
                                    }`}
                            >
                                {a.marks != null
                                    ? `Graded · ${a.marks}/${a.maximumMarks}`
                                    : "Submitted · awaiting grade"}
                            </span>
                        )}
                    </div>
                </div>

                {isStaff && (
                    <button
                        className="btn btn-delete-small"
                        onClick={async () => {
                            if (
                                !window.confirm(
                                    `Delete assignment “${a.title}”?`
                                )
                            ) {
                                return;
                            }

                            try {
                                await api.delete(
                                    `/assignments/${a.id}`
                                );

                                notify(
                                    "Assignment deleted.",
                                    "info"
                                );

                                await onChanged();
                            } catch (err) {
                                notify(
                                    errorMessage(err),
                                    "error"
                                );
                            }
                        }}
                    >
                        Delete
                    </button>
                )}
            </div>

            {isStudent &&
                courseEnrolled &&
                !a.isSubmitted && (
                    <div className="assignment-submit">
                        <textarea
                            rows="2"
                            value={text}
                            onChange={(e) =>
                                setText(e.target.value)
                            }
                            placeholder="Paste your answer, repository link or submission notes…"
                        />

                        <button
                            className="btn btn-primary btn-small"
                            disabled={busy || !text.trim()}
                            onClick={submit}
                        >
                            {busy
                                ? "Submitting…"
                                : "Submit assignment"}

                            <ArrowRight size={14} />
                        </button>
                    </div>
                )}

            {a.feedback && (
                <div className="feedback-note">
                    <b>Trainer feedback</b>
                    <p>{a.feedback}</p>
                </div>
            )}

            {isStaff && (
                <div className="assignment-staff">
                    <button
                        className="btn btn-outline btn-small"
                        onClick={loadSubs}
                    >
                        {subs
                            ? "Refresh submissions"
                            : "View submissions"}

                        <Users size={14} />
                    </button>

                    {subs?.map((s) => (
                        <div
                            className="submission-row"
                            key={s.id}
                        >
                            <div className="submission-copy">
                                <b>{s.studentName}</b>
                                <p>
                                    {s.text ||
                                        s.fileUrl ||
                                        "No text attached"}
                                </p>
                            </div>

                            <input
                                type="number"
                                min="0"
                                max={a.maximumMarks}
                                value={s.marks ?? marks}
                                onChange={(e) =>
                                    setMarks(Number(e.target.value))
                                }
                                aria-label="Marks awarded"
                            />

                            <input
                                className="feedback-input"
                                value={feedback}
                                onChange={(e) =>
                                    setFeedback(e.target.value)
                                }
                                placeholder="Feedback"
                                aria-label="Feedback"
                            />

                            <button
                                className="btn btn-primary btn-small"
                                onClick={async () => {
                                    try {
                                        await api.post(
                                            `/submissions/${s.id}/grade`,
                                            {
                                                marks: Number(
                                                    s.marks ?? marks
                                                ),
                                                feedback
                                            }
                                        );

                                        notify(
                                            `Graded ${s.studentName}'s work.`,
                                            "success"
                                        );

                                        await loadSubs();
                                        await onChanged();
                                    } catch (err) {
                                        notify(
                                            errorMessage(err),
                                            "error"
                                        );
                                    }
                                }}
                            >
                                Save grade
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </article>
    );
}


function ManualQuizCreate({ courseId, onDone, notify }) {
    const [open, setOpen] = useState(false);
    const [title, setTitle] = useState("");
    const [questions, setQuestions] = useState([blankQuestion()]);
    const [saving, setSaving] = useState(false);
    const [students, setStudents] = useState([]);
    const [assignedStudentIds, setAssignedStudentIds] = useState([]);
    const [studentsLoading, setStudentsLoading] = useState(false);

    useEffect(() => {
        if (!open) return;

        let cancelled = false;

        async function loadStudents() {
            setStudentsLoading(true);

            try {
                const { data } = await api.get(
                    `/courses/${courseId}/enrolled-students`
                );

                if (!cancelled) setStudents(data);
            } catch (err) {
                if (!cancelled) notify(errorMessage(err), "error");
            } finally {
                if (!cancelled) setStudentsLoading(false);
            }
        }

        loadStudents();

        return () => {
            cancelled = true;
        };
    }, [open, courseId, notify]);

    const changeQuestion = (i, key, value) => {
        setQuestions((all) =>
            all.map((q, index) =>
                index === i ? { ...q, [key]: value } : q
            )
        );
    };

    const changeOption = (i, oi, value) => {
        setQuestions((all) =>
            all.map((q, index) =>
                index === i
                    ? {
                        ...q,
                        options: q.options.map((o, j) =>
                            j === oi ? value : o
                        )
                    }
                    : q
            )
        );
    };

    function toggleStudent(studentId, checked) {
        setAssignedStudentIds((current) =>
            checked
                ? [...new Set([...current, studentId])]
                : current.filter((id) => id !== studentId)
        );
    }

    if (!open) {
        return (
            <button
                className="add-inline-card"
                onClick={() => setOpen(true)}
            >
                <span className="add-inline-icon">
                    <Plus size={18} />
                </span>
                <span>
                    <b>Create quiz manually</b>
                    <small>
                        Write questions and assign the quiz to students
                    </small>
                </span>
                <ArrowRight size={17} />
            </button>
        );
    }

    async function save(e) {
        e.preventDefault();

        if (studentsLoading) {
            notify("Wait for the enrolled students to load.", "info");
            return;
        }

        if (students.length === 0) {
            notify("Enroll students in this course first.", "error");
            return;
        }

        if (assignedStudentIds.length === 0) {
            notify("Select at least one student for this quiz.", "error");
            return;
        }

        setSaving(true);

        try {
            const passingMarks =
                questions.reduce(
                    (sum, q) => sum + Number(q.marks || 1),
                    0
                ) / 2;

            await api.post(`/courses/${courseId}/quizzes`, {
                title: title.trim(),
                description: "Course assessment",
                timeLimitMinutes: 20,
                passingMarks,
                publish: true,
                isAiGenerated: false,
                assignedStudentIds,
                questions: questions.map((q) => ({
                    ...q,
                    text: q.text.trim(),
                    options: q.options.map((o) => String(o).trim()),
                    marks: Number(q.marks || 1),
                    correctOptionIndex: Number(q.correctOptionIndex)
                }))
            });

            notify("Quiz published and assigned successfully.", "success");

            setOpen(false);
            setTitle("");
            setQuestions([blankQuestion()]);
            setAssignedStudentIds([]);

            await onDone();
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setSaving(false);
        }
    }

    return (
        <form className="panel create-inline-form" onSubmit={save}>
            <div className="panel-heading">
                <div>
                    <h3>Build a quiz</h3>
                    <p>Choose correct answers and assign students.</p>
                </div>

                <button
                    type="button"
                    className="btn btn-ghost btn-small"
                    onClick={() => setOpen(false)}
                >
                    Cancel
                </button>
            </div>

            <Field label="Quiz title" span>
                <input
                    required
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                />
            </Field>


            <div className="student-assignment-panel">
                <div className="student-assignment-header">
                    <div>
                        <h4>Assign quiz to students</h4>
                        <p>Select who can attempt this quiz.</p>
                    </div>

                    <button
                        type="button"
                        className="btn btn-outline btn-small"
                        disabled={studentsLoading || students.length === 0}
                        onClick={() =>
                            setAssignedStudentIds(
                                assignedStudentIds.length === students.length
                                    ? []
                                    : students.map((s) => Number(s.studentId))
                            )
                        }
                    >
                        {students.length > 0 &&
                            assignedStudentIds.length === students.length
                            ? "Deselect all"
                            : "Select all"}
                    </button>
                </div>

                {studentsLoading ? (
                    <p className="student-assignment-message">
                        Loading enrolled students...
                    </p>
                ) : students.length === 0 ? (
                    <p className="student-assignment-message">
                        No enrolled students found for this course.
                    </p>
                ) : (
                    <div className="student-assignment-list">
                        {students.map((student) => {
                            const studentId = Number(student.studentId);
                            const selected = assignedStudentIds.includes(studentId);
                            const initials = (student.name || "Student")
                                .trim()
                                .split(/\s+/)
                                .slice(0, 2)
                                .map((part) => part[0])
                                .join("")
                                .toUpperCase();

                            return (
                                <label
                                    key={studentId}
                                    className={`student-assignment-row ${selected ? "student-assignment-row-selected" : ""
                                        }`}
                                >
                                    <input
                                        className="student-assignment-checkbox"
                                        type="checkbox"
                                        checked={selected}
                                        onChange={(e) =>
                                            toggleStudent(studentId, e.target.checked)
                                        }
                                    />

                                    <span className="student-assignment-avatar">
                                        {initials}
                                    </span>

                                    <span className="student-assignment-details">
                                        <span className="student-assignment-name">
                                            {student.name}
                                        </span>
                                        <span className="student-assignment-email">
                                            {student.email}
                                        </span>
                                    </span>

                                    <span className="student-assignment-status">
                                        {selected ? "Selected" : "Select"}
                                    </span>
                                </label>
                            );
                        })}
                    </div>
                )}

                <div className="student-assignment-footer">
                    <span>
                        <Users size={16} />
                        {assignedStudentIds.length} of {students.length} students selected
                    </span>

                    {assignedStudentIds.length > 0 && (
                        <button
                            type="button"
                            className="student-clear-selection"
                            onClick={() => setAssignedStudentIds([])}
                        >
                            Clear selection
                        </button>
                    )}
                </div>
            </div>


            {questions.map((q, i) => (
                <div className="question-editor" key={i}>
                    <div className="question-editor-title">
                        <b>Question {i + 1}</b>

                        {questions.length > 1 && (
                            <button
                                type="button"
                                className="text-link text-danger"
                                onClick={() =>
                                    setQuestions((all) =>
                                        all.filter((_, j) => j !== i)
                                    )
                                }
                            >
                                Remove
                            </button>
                        )}
                    </div>

                    <input
                        required
                        placeholder="Question text"
                        value={q.text}
                        onChange={(e) =>
                            changeQuestion(i, "text", e.target.value)
                        }
                    />

                    {q.options.map((option, oi) => (
                        <label className="option-editor" key={oi}>
                            <input
                                type="radio"
                                name={`correct-${i}`}
                                checked={q.correctOptionIndex === oi}
                                onChange={() =>
                                    changeQuestion(i, "correctOptionIndex", oi)
                                }
                            />

                            <span>{String.fromCharCode(65 + oi)}</span>

                            <input
                                required
                                placeholder={`Option ${String.fromCharCode(65 + oi)}`}
                                value={option}
                                onChange={(e) =>
                                    changeOption(i, oi, e.target.value)
                                }
                            />
                        </label>
                    ))}

                    <input
                        placeholder="Explanation for learners"
                        value={q.explanation}
                        onChange={(e) =>
                            changeQuestion(i, "explanation", e.target.value)
                        }
                    />
                </div>
            ))}

            <div className="form-actions">
                <button
                    type="button"
                    className="btn btn-outline btn-small"
                    onClick={() =>
                        setQuestions((all) => [...all, blankQuestion()])
                    }
                >
                    <Plus size={14} />
                    Add question
                </button>

                <button
                    disabled={saving || studentsLoading}
                    className="btn btn-primary"
                >
                    {saving ? "Publishing..." : "Publish quiz"}
                    <ArrowRight size={15} />
                </button>
            </div>
        </form>
    );
}


function EnrollStudent({
    courseId,
    students,
    onDone,
    notify
}) {
    const [studentId, setStudentId] =
        useState("");

    async function enroll() {
        try {
            await api.post(
                `/courses/${courseId}/enroll`,
                {
                    studentId: Number(studentId)
                }
            );

            notify(
                "Student enrolled successfully.",
                "success"
            );

            setStudentId("");
            await onDone();
        } catch (err) {
            notify(errorMessage(err), "error");
        }
    }

    if (!students.length) return null;

    return (
        <div className="panel roster-enroll">
            <div>
                <b>Enroll a student</b>
                <p>
                    Assign an existing student to this course.
                </p>
            </div>

            <select
                value={studentId}
                onChange={(e) =>
                    setStudentId(e.target.value)
                }
            >
                <option value="">
                    Select a student…
                </option>

                {students.map((s) => (
                    <option
                        key={s.id}
                        value={s.studentId}
                    >
                        {s.name} · {s.email}
                    </option>
                ))}
            </select>

            <button
                className="btn btn-primary btn-small"
                disabled={!studentId}
                onClick={enroll}
            >
                Enroll
                <Plus size={14} />
            </button>
        </div>
    );
}


function AITools({ courseId, lessons, onDone, notify }) {
    const [topic, setTopic] = useState("");
    const [lessonId, setLessonId] = useState("");
    const [count, setCount] = useState(5);
    const [difficulty, setDifficulty] = useState("Beginner");
    const [generation, setGeneration] = useState(null);
    const [questions, setQuestions] = useState([]);
    const [busy, setBusy] = useState(false);
    const [summaryInput, setSummaryInput] = useState("");
    const [summary, setSummary] = useState(null);

    const [students, setStudents] = useState([]);
    const [assignedStudentIds, setAssignedStudentIds] = useState([]);
    const [studentsLoading, setStudentsLoading] = useState(false);

    useEffect(() => {
        let cancelled = false;

        async function loadStudents() {
            setStudentsLoading(true);
            try {
                const { data } = await api.get(
                    `/courses/${courseId}/enrolled-students`
                );
                if (!cancelled) setStudents(data);
            } catch (err) {
                if (!cancelled) notify(errorMessage(err), "error");
            } finally {
                if (!cancelled) setStudentsLoading(false);
            }
        }

        loadStudents();
        return () => { cancelled = true; };
    }, [courseId]);

    async function generateQuiz(e) {
        e.preventDefault();
        setBusy(true);
        try {
            const { data } = await api.post("/ai/generate-quiz", {
                topic,
                count: Number(count),
                difficulty,
                courseId,
                lessonId: lessonId ? Number(lessonId) : null
            });

            setGeneration(data.documentId);
            setQuestions(data.questions);
            setAssignedStudentIds([]);

            notify(
                data.usedOpenAi
                    ? "AI quiz generated. Review it before publishing."
                    : "Draft generated. Add OPENAI_API_KEY for live OpenAI generation.",
                data.usedOpenAi ? "success" : "info"
            );
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setBusy(false);
        }
    }

    function patchQuestion(index, key, value) {
        setQuestions((all) =>
            all.map((q, i) =>
                i === index ? { ...q, [key]: value } : q
            )
        );
    }

    function patchOption(index, optionIndex, value) {
        setQuestions((all) =>
            all.map((q, i) =>
                i === index
                    ? {
                        ...q,
                        options: q.options.map((o, j) =>
                            j === optionIndex ? value : o
                        )
                    }
                    : q
            )
        );
    }

    async function publishQuiz() {
        if (!generation) return;

        if (studentsLoading) {
            notify("Wait for the enrolled student list to load.", "info");
            return;
        }

        if (!students.length) {
            notify("Enroll students in this course first.", "error");
            return;
        }

        if (!assignedStudentIds.length) {
            notify("Select at least one student for this quiz.", "error");
            return;
        }

        if (!questions.length || questions.some((q) =>
            !q.text?.trim() ||
            !Array.isArray(q.options) ||
            q.options.length < 2 ||
            q.options.some((o) => !String(o).trim())
        )) {
            notify("Check that every question and answer option is filled in.", "error");
            return;
        }

        setBusy(true);
        try {
            await api.post(`/ai/quizzes/${generation}/publish`, {
                title: `${topic} — AI quiz`,
                description: `AI-generated assessment about ${topic}.`,
                timeLimitMinutes: 20,
                passingMarks: Math.max(
                    1,
                    questions.reduce((sum, q) =>
                        sum + Number(q.marks || 1), 0
                    ) / 2
                ),
                publish: true,
                isAiGenerated: true,
                assignedStudentIds,
                questions: questions.map((q) => ({
                    ...q,
                    text: q.text.trim(),
                    options: q.options.map((o) => String(o).trim()),
                    marks: Number(q.marks || 1),
                    correctOptionIndex: Number(q.correctOptionIndex)
                }))
            });

            notify("AI quiz published and assigned successfully.", "success");
            setGeneration(null);
            setQuestions([]);
            setAssignedStudentIds([]);
            await onDone();
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setBusy(false);
        }
    }

    async function generateSummary(e) {
        e.preventDefault();
        if (!summaryInput.trim()) return;

        setBusy(true);
        try {
            const { data } = await api.post("/ai/generate-summary", {
                content: summaryInput,
                courseId,
                lessonId: lessonId ? Number(lessonId) : null
            });

            setSummary(data);
            notify(
                data.usedOpenAi
                    ? "Summary generated and saved."
                    : "Summary generated with the offline summariser.",
                "success"
            );
        } catch (err) {
            notify(errorMessage(err), "error");
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className="ai-tools-grid">
            <section className="panel ai-tool-panel">
                <div className="ai-tool-heading">
                    <span className="ai-tool-icon ai-purple">
                        <BrainCircuit size={20} />
                    </span>
                    <div>
                        <h3>AI Quiz Generator</h3>
                        <p>Generate, review, edit and assign a quiz to students.</p>
                    </div>
                </div>

                <form className="ai-form" onSubmit={generateQuiz}>
                    <Field label="Topic">
                        <input
                            required
                            maxLength={300}
                            value={topic}
                            onChange={(e) => setTopic(e.target.value)}
                            placeholder="e.g. REST APIs, React hooks"
                        />
                    </Field>

                    <Field label="Lesson context (optional)">
                        <select value={lessonId} onChange={(e) => setLessonId(e.target.value)}>
                            <option value="">Whole course / topic only</option>
                            {lessons.map((l) => (
                                <option key={l.id} value={l.id}>{l.title}</option>
                            ))}
                        </select>
                    </Field>

                    <div className="form-grid">
                        <Field label="Questions">
                            <input
                                type="number"
                                min="1"
                                max="20"
                                value={count}
                                onChange={(e) => setCount(e.target.value)}
                            />
                        </Field>
                        <Field label="Difficulty">
                            <select value={difficulty} onChange={(e) => setDifficulty(e.target.value)}>
                                <option>Beginner</option>
                                <option>Intermediate</option>
                                <option>Advanced</option>
                            </select>
                        </Field>
                    </div>

                    <button disabled={busy} className="btn btn-primary btn-block">
                        {busy ? "Generating…" : "Generate quiz with AI"}
                        <Sparkles size={16} />
                    </button>
                </form>

                {generation && (
                    <div className="generated-questions">
                        <div className="generated-head">
                            <b>Review your AI draft</b>
                            <span className="ai-generated-pill">
                                <Sparkles size={12} /> Draft saved in MongoDB
                            </span>
                        </div>

                        {questions.map((q, i) => (
                            <div className="generated-question" key={i}>
                                <div className="generated-q-title">
                                    <span>Q{i + 1}</span>
                                    <button
                                        type="button"
                                        className="text-link text-danger"
                                        onClick={() => setQuestions((all) =>
                                            all.filter((_, j) => j !== i)
                                        )}
                                    >
                                        Delete
                                    </button>
                                </div>

                                <textarea
                                    rows="2"
                                    value={q.text}
                                    onChange={(e) => patchQuestion(i, "text", e.target.value)}
                                    required
                                />

                                {q.options.map((option, oi) => (
                                    <label className="option-editor" key={oi}>
                                        <input
                                            type="radio"
                                            name={`ai-correct-${i}`}
                                            checked={q.correctOptionIndex === oi}
                                            onChange={() => patchQuestion(i, "correctOptionIndex", oi)}
                                        />
                                        <span>{String.fromCharCode(65 + oi)}</span>
                                        <input
                                            required
                                            value={option}
                                            onChange={(e) => patchOption(i, oi, e.target.value)}
                                        />
                                    </label>
                                ))}

                                <textarea
                                    rows="2"
                                    value={q.explanation || ""}
                                    onChange={(e) => patchQuestion(i, "explanation", e.target.value)}
                                    placeholder="Answer explanation"
                                />
                            </div>
                        ))}


                        <div className="student-assignment-panel">
                            <div className="student-assignment-header">
                                <div>
                                    <h4>Assign AI quiz to students</h4>
                                    <p>Select enrolled students who can attempt this quiz.</p>
                                </div>

                                <button
                                    type="button"
                                    className="btn btn-outline btn-small"
                                    disabled={studentsLoading || students.length === 0}
                                    onClick={() => {
                                        const allSelected =
                                            students.length > 0 &&
                                            students.every((s) =>
                                                assignedStudentIds.includes(Number(s.studentId))
                                            );

                                        setAssignedStudentIds(
                                            allSelected
                                                ? []
                                                : students.map((s) => Number(s.studentId))
                                        );
                                    }}
                                >
                                    {students.length > 0 &&
                                        students.every((s) =>
                                            assignedStudentIds.includes(Number(s.studentId))
                                        )
                                        ? "Deselect all"
                                        : "Select all"}
                                </button>
                            </div>

                            {studentsLoading ? (
                                <p className="student-assignment-message">
                                    Loading enrolled students...
                                </p>
                            ) : students.length === 0 ? (
                                <p className="student-assignment-message">
                                    No enrolled students found for this course.
                                </p>
                            ) : (
                                <div className="student-assignment-list">
                                    {students.map((student) => {
                                        const studentId = Number(student.studentId);
                                        const selected = assignedStudentIds.includes(studentId);

                                        const initials = (student.name || "Student")
                                            .trim()
                                            .split(/\s+/)
                                            .slice(0, 2)
                                            .map((part) => part[0])
                                            .join("")
                                            .toUpperCase();

                                        return (
                                            <label
                                                key={studentId}
                                                className={`student-assignment-row ${selected
                                                        ? "student-assignment-row-selected"
                                                        : ""
                                                    }`}
                                            >
                                                <input
                                                    className="student-assignment-checkbox"
                                                    type="checkbox"
                                                    checked={selected}
                                                    onChange={(e) =>
                                                        setAssignedStudentIds((current) =>
                                                            e.target.checked
                                                                ? [...new Set([...current, studentId])]
                                                                : current.filter(
                                                                    (id) => id !== studentId
                                                                )
                                                        )
                                                    }
                                                />

                                                <span className="student-assignment-avatar">
                                                    {initials}
                                                </span>

                                                <span className="student-assignment-details">
                                                    <span className="student-assignment-name">
                                                        {student.name}
                                                    </span>
                                                    <span className="student-assignment-email">
                                                        {student.email}
                                                    </span>
                                                </span>

                                                <span className="student-assignment-status">
                                                    {selected ? "Selected" : "Select"}
                                                </span>
                                            </label>
                                        );
                                    })}
                                </div>
                            )}

                            <div className="student-assignment-footer">
                                <span>
                                    <Users size={16} />
                                    {assignedStudentIds.length} of {students.length} students selected
                                </span>

                                {assignedStudentIds.length > 0 && (
                                    <button
                                        type="button"
                                        className="student-clear-selection"
                                        onClick={() => setAssignedStudentIds([])}
                                    >
                                        Clear selection
                                    </button>
                                )}
                            </div>
                        </div>


                        <div className="form-actions">
                            <button
                                type="button"
                                className="btn btn-ghost btn-small"
                                onClick={() => {
                                    setGeneration(null);
                                    setQuestions([]);
                                    setAssignedStudentIds([]);
                                }}
                            >
                                Discard draft
                            </button>

                            <button
                                type="button"
                                disabled={busy || !questions.length || studentsLoading}
                                className="btn btn-success"
                                onClick={publishQuiz}
                            >
                                {busy ? "Publishing…" : "Publish quiz"}
                                <ArrowRight size={15} />
                            </button>
                        </div>
                    </div>
                )}
            </section>

            <section className="panel ai-tool-panel">
                <div className="ai-tool-heading">
                    <span className="ai-tool-icon ai-mint">
                        <Sparkles size={20} />
                    </span>
                    <div>
                        <h3>Course summary</h3>
                        <p>Generate concise notes, key points and concepts.</p>
                    </div>
                </div>

                <form className="ai-form" onSubmit={generateSummary}>
                    <Field label="Lesson source (optional)">
                        <select value={lessonId} onChange={(e) => setLessonId(e.target.value)}>
                            <option value="">Custom source text</option>
                            {lessons.map((l) => (
                                <option key={l.id} value={l.id}>{l.title}</option>
                            ))}
                        </select>
                    </Field>

                    <label className="field-label">
                        Course content
                        <textarea
                            className="ai-source-area"
                            required
                            value={summaryInput}
                            onChange={(e) => setSummaryInput(e.target.value)}
                            placeholder="Paste or write lesson content to summarise…"
                            rows="7"
                        />
                    </label>

                    <div className="form-actions">
                        <button
                            disabled={busy || !summaryInput.trim()}
                            className="btn btn-primary"
                        >
                            Generate summary <Sparkles size={15} />
                        </button>

                        {summary && (
                            <button
                                type="button"
                                className="btn btn-outline btn-small"
                                onClick={async () => {
                                    await navigator.clipboard.writeText(summary.summary);
                                    notify("Summary copied to clipboard.", "success");
                                }}
                            >
                                Copy notes
                            </button>
                        )}
                    </div>
                </form>

                {summary && (
                    <div className="summary-result">
                        <div className="summary-result-title">
                            <b>Study notes</b>
                            <span className="ai-generated-pill">
                                {summary.usedOpenAi ? "OpenAI" : "Offline"}
                            </span>
                        </div>

                        <p>{summary.summary}</p>

                        {summary.keyPoints?.length > 0 && (
                            <>
                                <b>Key points</b>
                                <ul>
                                    {summary.keyPoints.map((point, i) => (
                                        <li key={i}>{point}</li>
                                    ))}
                                </ul>
                            </>
                        )}

                        {summary.concepts?.length > 0 && (
                            <div className="concept-tags">
                                {summary.concepts.map((concept) => (
                                    <span key={concept}>{concept}</span>
                                ))}
                            </div>
                        )}
                    </div>
                )}
            </section>
        </div>
    );
}


function Field({
    label,
    children,
    span
}) {
    return (
        <label
            className={`form-field ${span ? "form-span-2" : ""
                }`}
        >
            <span>{label}</span>
            {children}
        </label>
    );
}

function EmptyCard({
    icon: Icon,
    title,
    text
}) {
    return (
        <div className="panel empty-results">
            <div className="empty-state-icon">
                <Icon size={21} />
            </div>

            <h3>{title}</h3>
            <p>{text}</p>
        </div>
    );
}

function ChartIcon() {
    return <Sparkles size={18} />;
}